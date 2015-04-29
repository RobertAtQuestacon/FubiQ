// ****************************************************************************************
//
// Template Recognizer
// ---------------------------------------------------------
// Copyright (C) 2010-2015 Felix Kistler
//
// This software is distributed under the terms of the Eclipse Public License v1.0.
// A copy of the license may be obtained at: http://www.eclipse.org/org/documents/epl-v10.html
//
// ****************************************************************************************
#include "TemplateRecognizer.h"
#include "../FubiGMR.h"

#include "../FubiImageProcessing.h"
#include "../FubiConfig.h"

using namespace Fubi;

TemplateRecognizer::TemplateRecognizer(float maxDistance, Fubi::DistanceMeasure::Measure distanceMeasure, float maxRotation, bool aspectInvariant,
	unsigned int ignoreAxes, bool useOrientations, bool applyResampling, const Fubi::BodyMeasurementDistance* bodyMeasures,
	Fubi::BodyMeasurement::Measurement measuringUnit, float minConfidence, bool useLocalTransformations, bool useFilteredData)
	: m_maxDist(maxDistance), m_distanceMeasure(distanceMeasure), m_maxRotation(degToRad(maxRotation)), m_aspectInvariant(aspectInvariant),
	m_ignoreAxes(ignoreAxes), m_numIgnoredAxes(calcNumIgnoredAxesAndMinAabbSize()), m_useOrientations(useOrientations), m_useDTW(false), m_applyResampling(applyResampling), m_useGMR(false),
	m_numGMRStates(0), m_measuringUnit(measuringUnit), m_useLocalTransformations(useLocalTransformations), m_maxSampleSize(0),
	IGestureRecognizer(false, minConfidence, useFilteredData)
{
	// Constructor for usage in sub classes that do their own  training
}

TemplateRecognizer::TemplateRecognizer(const std::vector<Fubi::SkeletonJoint::Joint>& joints, const std::vector<Fubi::SkeletonJoint::Joint>& relJoints,
	const std::vector<std::deque<Fubi::TrackingData>>& trainingData, float maxDistance, Fubi::DistanceMeasure::Measure distanceMeasure /*= Fubi::DistanceMeasure::Euclidean*/,
	float maxRotation /*= 45.0f*/, bool aspectInvariant /*= false*/, unsigned int ignoreAxes /*= Fubi::CoordinateAxis::NONE*/, bool useOrientations /*=false*/,
	bool useDTW /*= true*/, bool applyResampling /*= false*/, bool useGMR /*= true*/, unsigned int numGMRStates /*= 5*/,
	const Fubi::BodyMeasurementDistance* bodyMeasures /*= 0x0*/, Fubi::BodyMeasurement::Measurement measuringUnit /*= Fubi::BodyMeasurement::NUM_MEASUREMENTS*/,
	float minConfidence /*= -1.0f*/, bool useLocalTransformations /*= false*/, bool useFilteredData /*= false*/)
	: m_maxDist(maxDistance), m_distanceMeasure(distanceMeasure), m_maxRotation(degToRad(maxRotation)), m_aspectInvariant(aspectInvariant),
	m_ignoreAxes(ignoreAxes), m_numIgnoredAxes(calcNumIgnoredAxesAndMinAabbSize()), m_useOrientations(useOrientations), m_useDTW(useDTW), m_applyResampling(applyResampling), m_useGMR(useGMR),
	m_numGMRStates(numGMRStates), m_measuringUnit(measuringUnit), m_useLocalTransformations(useLocalTransformations), m_maxSampleSize(0),
	IGestureRecognizer(false, minConfidence, useFilteredData)
{
	if (joints.size() > 0 && trainingData.size() > 0)
	{
		const BodyMeasurementDistance* measure = (m_measuringUnit == BodyMeasurement::NUM_MEASUREMENTS)
			? 0x0 : &(bodyMeasures[m_measuringUnit]);

		for (unsigned int j = 0; j < joints.size(); ++j)
		{
			TemplateData templateData;
			templateData.m_joint = joints[j];
			templateData.m_relJoint = (relJoints.size() > j) ? relJoints[j] : SkeletonJoint::NUM_JOINTS;

			// Find and convert the data
			std::vector<std::vector<Vec3f>> convertedData;
			std::vector<Vec3f> indicativeOrients;
			extractTrainingData(trainingData, templateData.m_joint, templateData.m_relJoint, measure, convertedData, &indicativeOrients);

			// Calculate mean and covariance matrices for the coordinate points and the indicative orients
			calculateMeanAndCovs(templateData, convertedData, indicativeOrients);

			// Now the data is ready
			m_trainingData.push_back(templateData);
		}
		unsigned int dataSize = m_trainingData.front().m_data.size();
		m_maxWarpingDistance = dataSize / 2;
		// Resize dtw distance buffer to it's maximum required size and fill it with MaxFloats
		m_dtwDistanceBuffer.resize(dataSize, std::vector<float>(dataSize + m_maxWarpingDistance, Math::MaxFloat));
	}
}
TemplateRecognizer::TemplateRecognizer(const std::vector<Fubi::SkeletonHandJoint::Joint>& joints, const std::vector<Fubi::SkeletonHandJoint::Joint>& relJoints,
	const std::vector<std::deque<Fubi::TrackingData>>& trainingData, float maxDistance, Fubi::DistanceMeasure::Measure distanceMeasure /*= Fubi::DistanceMeasure::Euclidean*/,
	float maxRotation /*= 45.0f*/, bool aspectInvariant /*= false*/, unsigned int ignoreAxes /*= Fubi::CoordinateAxis::NONE*/, bool useOrientations /*=false*/,
	bool useDTW /*= true*/, bool applyResampling /*= false*/, bool useGMR /*= true*/, unsigned int numGMRStates /*= 5*/,
	float minConfidence /*= -1.0f*/, bool useLocalTransformations /*= false*/, bool useFilteredData /*= false*/)
	: m_maxDist(maxDistance), m_distanceMeasure(distanceMeasure), m_maxRotation(degToRad(maxRotation)), m_aspectInvariant(aspectInvariant),
	m_ignoreAxes(ignoreAxes), m_numIgnoredAxes(calcNumIgnoredAxesAndMinAabbSize()), m_useOrientations(useOrientations), m_useDTW(useDTW), m_applyResampling(applyResampling), m_useGMR(useGMR),
	m_numGMRStates(numGMRStates), m_measuringUnit(Fubi::BodyMeasurement::NUM_MEASUREMENTS), m_useLocalTransformations(useLocalTransformations), m_maxSampleSize(0),
	IGestureRecognizer(false, minConfidence, useFilteredData)
{
	if (joints.size() > 0 && trainingData.size() > 0)
	{
		for (unsigned int j = 0; j < joints.size(); ++j)
		{
			TemplateData templateData;
			templateData.m_joint = (joints[j] != SkeletonHandJoint::NUM_JOINTS) ? (SkeletonJoint::Joint) joints[j] : SkeletonJoint::NUM_JOINTS;
			templateData.m_relJoint = (relJoints.size() > j && relJoints[j] != SkeletonHandJoint::NUM_JOINTS) ? (SkeletonJoint::Joint) relJoints[j] : SkeletonJoint::NUM_JOINTS;

			// Find and convert the data
			std::vector<std::vector<Vec3f>> convertedData;
			std::vector<Vec3f> indicativeOrients;
			extractTrainingData(trainingData, templateData.m_joint, templateData.m_relJoint, 0x0, convertedData, &indicativeOrients);

			// Calculate mean and covariance matrices for the coordinate points and the indicative orients
			calculateMeanAndCovs(templateData, convertedData, indicativeOrients);
			// Now the data is ready
			m_trainingData.push_back(templateData);
		}
		unsigned int dataSize = m_trainingData.front().m_data.size();
		m_maxWarpingDistance = dataSize / 2;
		// Resize dtw distance buffer to it's maximum required size and fill it with MaxFloats
		m_dtwDistanceBuffer.resize(dataSize, std::vector<float>(dataSize + m_maxWarpingDistance, Math::MaxFloat));
	}
}

unsigned int TemplateRecognizer::calcNumIgnoredAxesAndMinAabbSize()
{
	m_numIgnoredAxes = 0;
	if (m_ignoreAxes & CoordinateAxis::X)
		++m_numIgnoredAxes;
	if (m_ignoreAxes & CoordinateAxis::Y)
		++m_numIgnoredAxes;
	if (m_ignoreAxes & CoordinateAxis::Z)
		++m_numIgnoredAxes;

	m_minAabbSize = powf((m_useOrientations ? 0.05f : 50.0f), (3.0f - m_numIgnoredAxes));

	return m_numIgnoredAxes;
}

void TemplateRecognizer::extractTrainingData(const std::vector<std::deque<Fubi::TrackingData>>& trainingData, Fubi::SkeletonJoint::Joint joint, Fubi::SkeletonJoint::Joint relJoint, const BodyMeasurementDistance* measure,
	std::vector<std::vector<Vec3f >>& convertedData, std::vector<Fubi::Vec3f>* indicativeOrients /*= 0x0*/)
{
	// Calculate mean and max size of training data
	unsigned int meanSize = 0;
	for (auto iter = trainingData.begin(); iter != trainingData.end(); ++iter)
	{
		meanSize += iter->size();
		m_maxSampleSize = maxi(m_maxSampleSize, iter->size());
	}
	meanSize /= trainingData.size();

	for (auto iter = trainingData.begin(); iter != trainingData.end(); ++iter)
	{
		std::vector<Fubi::Vec3f> rawData, resampledData, *data;
		for (auto iter2 = iter->begin(); iter2 != iter->end(); ++iter2)
		{
			Vec3f dataPoint(Math::NO_INIT);
			if (m_useLocalTransformations) // local transformations are not provided in the recording, so we need to calculate them...
				calculateLocalTransformations(iter2->jointPositions, iter2->jointOrientations, iter2->localJointPositions, iter2->localJointOrientations);
			if (m_useOrientations)
			{
				const SkeletonJointOrientation* jointOrient = m_useLocalTransformations
					? &(iter2->localJointOrientations[joint])
					: &(iter2->jointOrientations[joint]);
				// We normalize the rotations later, so we don't care if they are in degrees or radians
				dataPoint = jointOrient->m_orientation.getRot(false);
			}
			else
			{
				const SkeletonJointPosition* jointPos = m_useLocalTransformations
					? &(iter2->localJointPositions[joint])
					: &(iter2->jointPositions[joint]);
				const SkeletonJointPosition* relJointPos = 0x0;
				if (relJoint != SkeletonJoint::NUM_JOINTS)
				{
					relJointPos = m_useLocalTransformations
						? &(iter2->localJointPositions[relJoint])
						: &(iter2->jointPositions[relJoint]);
				}
				// Calculate the final coordinate values to be used for training
				dataPoint = jointPos->m_position;
				if (relJointPos)
					dataPoint -= relJointPos->m_position;
				if (measure && measure->m_dist > Math::Epsilon)
					dataPoint /= measure->m_dist;
			}
			// Always reduce the axes before the normalizations
			rawData.push_back(reduceAxes(dataPoint));
		}

		// Apply normalizations simliar to the 1$ Recognizer
		// (but no rotation normalization, as we want to keep rotation differences)
		// TODO: 1$ equidistant resampling?
		if (rawData.size() == meanSize)
			data = &rawData;
		else
		{
			// Resampling if necessary
			hermitteSplineRescale(rawData, meanSize, resampledData);
			data = &resampledData;
		}
		if (!m_useOrientations)
		{
			// Translate centroid to origin
			translate(*data, -centroid(*data));
			// Calculate indicative angle (=angle of first point); Before Scaling!
			if (indicativeOrients)
				indicativeOrients->push_back(calculateIndicativeOrient(*data));
		}
		// Scale data to a unit cube (only normalizaton done for orientations as well),
		// but keep aspect ratio by fitting the largest side to length one if aspect invariance is deactivated
		scale(*data, Vec3f(1.0f, 1.0f, 1.0f), 0.0f, !m_aspectInvariant, !m_aspectInvariant);

		convertedData.push_back(*data);
	}
}

void TemplateRecognizer::calculateMeanAndCovs(TemplateData& data, const std::vector<std::vector<Fubi::Vec3f>>& convertedData, const std::vector<Fubi::Vec3f>& indicativeOrients)
{
	if (convertedData.size() == 1)
	{
		// Directly use the single element
		data.m_data = convertedData[0];
		if (!m_useOrientations)
			data.m_indicativeOrient = indicativeOrients[0];
	}
	else
	{
		// Calculate the mean indicative orientation
		if (!m_useOrientations)
			data.m_indicativeOrient = calculateMean(indicativeOrients);

		// Get dimensions
		float numSamples = (float)convertedData.size();
		unsigned int size = convertedData[0].size();

		// Resize data and covs
		data.m_data.resize(size);
		float nulls[] = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		Matrix3f nullMatrix(nulls);
		data.m_inverseCovs.resize(size, nullMatrix);

		// Calculate the mean of the data and its covariance matrix for each element of the converted data
		if (m_useGMR)
		{
			// Use GMR to do this
			FubiGMR g(convertedData, m_numGMRStates);

			// Apply EM to get the GMM
			g.calculateGMM(MaxGMMEMIterations);

			// Apply GMR to get new general mean an covariance values
			g.calculateGMR(data.m_data, data.m_inverseCovs);
		}
		else
		{
			// Calculate them by hand for the raw data
			for (unsigned int i = 0; i < size; ++i)
			{
				for (auto iter = convertedData.begin(); iter != convertedData.end(); ++iter)
				{
					Vec3f value = iter->at(i);
					data.m_data[i] += value;
					for (int j = 0; j < 3; ++j)
					{
						for (int k = j; k < 3; ++k)
						{
							float mult = value[j] * value[k];
							data.m_inverseCovs[i].c[j][k] += mult;
						}
					}
				}
				data.m_data[i] /= numSamples;
				for (int j = 0; j < 3; ++j)
				{
					for (int k = j; k < 3; ++k)
					{
						data.m_inverseCovs[i].c[j][k] /= numSamples;
						data.m_inverseCovs[i].c[j][k] -= data.m_data[i][j] * data.m_data[i][k];

						if (j != k)
							data.m_inverseCovs[i].c[k][j] = data.m_inverseCovs[i].c[j][k];
					}
				}
				// Don't forget to invert the matrix
				data.m_inverseCovs[i] = data.m_inverseCovs[i].inverted();
			}
		}
	}

#ifdef FUBI_TEMPLATEREC_DEBUG_DRAWING
	FubiImageProcessing::showPlot(data.m_data, 640, 480, "trainingData");
#endif
}

Vec3f TemplateRecognizer::calculateIndicativeOrient(const std::vector<Fubi::Vec3f>& data)
{
	if (m_numIgnoredAxes > 1)
		return NullVec;

	const Vec3f& firstPoint = data.front();
	Vec3f orient;
	if (m_numIgnoredAxes == 1)
	{
		// Only calculate angle around the ignored axes
		float a, b;
		float* angle;
		if (m_ignoreAxes & CoordinateAxis::X)
		{
			a = firstPoint.y;
			b = firstPoint.z;
			angle = &orient.x;
		}
		else if (m_ignoreAxes & CoordinateAxis::Y)
		{
			a = firstPoint.x;
			b = firstPoint.z;
			angle = &orient.y;
		}
		else if (m_ignoreAxes & CoordinateAxis::Z)
		{
			a = firstPoint.x;
			b = firstPoint.y;
			angle = &orient.z;
		}
		// Calc angle from (1,0) to (a,b)
		if (a != 0 || b != 0)
			*angle = atan2f(b, a);
	}
	else
	{
		// Calc angle around x and y
		orient = firstPoint.toRotation();

		// TODO: The furthest point might already be too different even for slight variations of certain shapes
		// Is it necessary to  use have the third angle?
		// Further, the below calculation does not seem to be correct...
		//
		// For the z rotation, we take the point of the first half points furthest away from the axis spanned up by the first point
		//float maxDistSq = 0;
		//Vec3f origin(0, 0, 0);
		//Vec3f furthestDir;
		//auto end = data.begin() + (data.size() / 2);
		//for (auto iter = data.begin()+1; iter != end; ++iter)
		//{
		//	Vec3f intersect = closestPointFromPointToRay(*iter, origin, firstPoint);
		//	float distSq = (*iter - intersect).length2();
		//	if (distSq > maxDistSq)
		//	{
		//		maxDistSq = distSq;
		//		furthestDir = *iter - intersect;
		//	}
		//}
		//// Now calculate the angle between this direction and the rotated x axis vector
		//Vec3f rotatedX1Vec = Matrix3f::RotMat(orient) * Vec3f(1.0f, 0, 0);
		//furthestDir.normalize(); // Normalize for easier calculations
		//float DirDotUp = furthestDir.dot(rotatedX1Vec);
		//Vec3f DirCrossUp = furthestDir.cross(rotatedX1Vec);
		//orient.z = atan2f(DirCrossUp.length(), DirDotUp);
		//if (DirCrossUp.dot(firstPoint) < 0)
		//	orient.z *= -1.0f;
	}

	return orient;
}

Fubi::Vec3f& TemplateRecognizer::reduceAxes(Fubi::Vec3f& vec)
{
	if (m_ignoreAxes & CoordinateAxis::X)
		vec.x = 0;
	if (m_ignoreAxes & CoordinateAxis::Y)
		vec.y = 0;
	if (m_ignoreAxes & CoordinateAxis::Z)
		vec.z = 0;
	return vec;
}

bool TemplateRecognizer::hasTrackingErrors(Fubi::SkeletonJoint::Joint joint, Fubi::SkeletonJoint::Joint relJoint, const std::deque<Fubi::TrackingData*>* data)
{
	const Fubi::TrackingData* lastFrame = data->back();
	const Fubi::TrackingData* firstFrame = data->front();
	// Now check confidences for first and last frame only
	if (lastFrame->jointPositions[joint].m_confidence < m_minConfidence ||
		(relJoint != SkeletonJoint::NUM_JOINTS && lastFrame->jointPositions[relJoint].m_confidence < m_minConfidence) ||
		firstFrame->jointPositions[joint].m_confidence < m_minConfidence ||
		(relJoint != SkeletonJoint::NUM_JOINTS && firstFrame->jointPositions[relJoint].m_confidence < m_minConfidence))
	{
		return true;
	}
	return false;
}

RecognitionResult::Result TemplateRecognizer::recognizeOn(const FubiUser* user, Fubi::RecognitionCorrectionHint* correctionHint /*= 0x0*/)
{
	Fubi::RecognitionResult::Result result = Fubi::RecognitionResult::NOT_RECOGNIZED;

	if (user != 0x0)
	{
		const std::deque<Fubi::TrackingData*>* data = user->trackingData();
		if (m_useFilteredData)
			data = user->filteredTrackingData();

		// Check if data frame is already long enough
		if (m_trainingData.size() > 0 && m_trainingData.front().m_data.size() > 0 && data->size() >= (m_trainingData.front().m_data.size()-m_maxWarpingDistance))
		{
			const BodyMeasurementDistance* measure = 0x0;
			if (m_measuringUnit != BodyMeasurement::NUM_MEASUREMENTS)
				measure = &user->bodyMeasurements()[m_measuringUnit];

			float distanceSum = 0;
			auto end = m_trainingData.end();
			for (auto iter = m_trainingData.begin(); iter != end; ++iter)
			{
				if (hasTrackingErrors(iter->m_joint, iter->m_relJoint, data))
				{
					result = RecognitionResult::TRACKING_ERROR;
					break;
				}
				else
				{
					// Extract the relevant part of the data
					int extractSize = mini(m_maxSampleSize, data->size());
					m_convertedData.clear();
					m_convertedData.reserve(extractSize);
					auto end = data->end();
					for (auto iter2 = data->begin() + (data->size() - extractSize); iter2 != end; ++iter2)
					{
						if (m_useOrientations)
						{
							const SkeletonJointOrientation* jointOrient = m_useLocalTransformations
								? &((*iter2)->localJointOrientations[iter->m_joint])
								: &((*iter2)->jointOrientations[iter->m_joint]);
							// Don't forget to reduce the axes
							Vec3f rot = jointOrient->m_orientation.getRot(false);
							m_convertedData.push_back(reduceAxes(rot));
						}
						else
						{
							Vec3f pos = m_useLocalTransformations
								? (*iter2)->localJointPositions[iter->m_joint].m_position
								: (*iter2)->jointPositions[iter->m_joint].m_position;
							if (iter->m_relJoint != SkeletonJoint::NUM_JOINTS)
							{
								if (m_useLocalTransformations)
									pos -= (*iter2)->localJointPositions[iter->m_relJoint].m_position;
								else
									pos -= (*iter2)->jointPositions[iter->m_relJoint].m_position;
							}
							if (measure && measure->m_confidence > m_minConfidence && measure->m_dist > Math::Epsilon)
							{
								// Apply measuring unit
								pos /= measure->m_dist;
							}
							// Don't forget to reduce the axes
							m_convertedData.push_back(reduceAxes(pos));
						}
					}
					float jointsDist = calculateDistance(iter->m_data, iter->m_indicativeOrient, &m_convertedData, &iter->m_inverseCovs);
					if (jointsDist < Math::MaxFloat - distanceSum)
						distanceSum += jointsDist;
					else
					{
						distanceSum = Math::MaxFloat;
						// Set correction hint for joint that failed
						if (correctionHint)
							correctionHint->m_joint = iter->m_joint;
						break;
					}
				}
			}
			if (result != RecognitionResult::TRACKING_ERROR)
			{
				if (distanceSum >= 0)
				{
					float dist = (distanceSum < Math::MaxFloat) ? (distanceSum / m_trainingData.size()) : Math::MaxFloat;
					if (dist < m_maxDist)
						result = Fubi::RecognitionResult::RECOGNIZED;
					// Always set correction hint to notify about the current distance
					if (correctionHint)
					{
						correctionHint->m_dist = dist;
						correctionHint->m_changeType = RecognitionCorrectionHint::FORM;
						correctionHint->m_changeDirection = RecognitionCorrectionHint::DIFFERENT;
					}
				}
			}
		}
		else
			result = Fubi::RecognitionResult::TRACKING_ERROR;
	}
	return result;
}


RecognitionResult::Result TemplateRecognizer::recognizeOn(const FubiHand* hand, Fubi::RecognitionCorrectionHint* correctionHint /*= 0x0*/)
{
	Fubi::RecognitionResult::Result result = Fubi::RecognitionResult::NOT_RECOGNIZED;
	if (hand != 0x0)
	{
		const std::deque<Fubi::TrackingData*>* data = hand->trackingData();
		if (m_useFilteredData)
			data = hand->filteredTrackingData();

		// Check if data frame is already long enough
		if (m_trainingData.size() > 0 && m_trainingData.front().m_data.size() > 0 && data->size() >= (m_trainingData.front().m_data.size() - m_maxWarpingDistance))
		{
			float distanceSum = 0;
			auto end = m_trainingData.end();
			for (auto iter = m_trainingData.begin(); iter != end; ++iter)
			{
				if (hasTrackingErrors(iter->m_joint, iter->m_relJoint, data))
				{
					result = RecognitionResult::TRACKING_ERROR;
					break;
				}
				else
				{
					// Extract the relevant part of the data
					int extractSize = mini(iter->m_data.size(), data->size());
					m_convertedData.clear();
					m_convertedData.reserve(extractSize);
					auto end = data->end();
					for (auto iter2 = data->begin() + (data->size() - extractSize); iter2 != end; ++iter2)
					{
						if (m_useOrientations)
						{
							const SkeletonJointOrientation* jointOrient = m_useLocalTransformations
								? &((*iter2)->localJointOrientations[iter->m_joint])
								: &((*iter2)->jointOrientations[iter->m_joint]);
							// Don't forget to reduce the axes
							Vec3f rot = jointOrient->m_orientation.getRot(false);
							m_convertedData.push_back(reduceAxes(rot));
						}
						else
						{
							Vec3f pos = m_useLocalTransformations
								? (*iter2)->localJointPositions[iter->m_joint].m_position
								: (*iter2)->jointPositions[iter->m_joint].m_position;
							if (iter->m_relJoint != SkeletonJoint::NUM_JOINTS)
							{
								if (m_useLocalTransformations)
									pos -= (*iter2)->localJointPositions[iter->m_relJoint].m_position;
								else
									pos -= (*iter2)->jointPositions[iter->m_relJoint].m_position;
							}
							// Don't forget to reduce the axes
							m_convertedData.push_back(reduceAxes(pos));
						}
					}
					float jointsDist = calculateDistance(iter->m_data, iter->m_indicativeOrient, &m_convertedData, &iter->m_inverseCovs);
					if (jointsDist < Math::MaxFloat - distanceSum)
						distanceSum += jointsDist;
					else
					{
						distanceSum = Math::MaxFloat;
						// Set correction hint for joint that failed
						if (correctionHint)
							correctionHint->m_joint = iter->m_joint;
						break;
					}
				}
			}
			if (result != RecognitionResult::TRACKING_ERROR)
			{
				if (distanceSum >= 0)
				{
					float dist = (distanceSum < Math::MaxFloat) ? (distanceSum / m_trainingData.size()) : Math::MaxFloat;
					if (dist < m_maxDist)
						result = Fubi::RecognitionResult::RECOGNIZED;
					// Always set correction hint to notify about current distance
					if (correctionHint)
					{
						correctionHint->m_dist = dist;
						correctionHint->m_changeType = RecognitionCorrectionHint::FORM;
						correctionHint->m_changeDirection = RecognitionCorrectionHint::DIFFERENT;
					}
				}
			}
		}
		else
			result = Fubi::RecognitionResult::TRACKING_ERROR;
	}
	return result;
}

float TemplateRecognizer::calculateDistance(const std::vector<Fubi::Vec3f>& trainingData, const Fubi::Vec3f& indicativeOrient, std::vector<Fubi::Vec3f>* testData, const std::vector<Fubi::Matrix3f>* covariances)
{
	float dist = Math::MaxFloat;
	std::vector<Fubi::Vec3f> tempData; // Used if resampling is activated

#ifdef FUBI_TEMPLATEREC_DEBUG_DRAWING
	static unsigned int drawCounter = 0;
	bool drawPoints = drawCounter++ % 15 == 0;
	if (drawPoints)
		FubiImageProcessing::showPlot(*testData, 640, 480, "rawInput");
#endif

	// Now apply normalizations simliar to the 1$ Recognizer
	if (m_applyResampling)
	{
		// Apply rescaling on the tempData vector
		Fubi::hermitteSplineRescale(*testData, trainingData.size(), tempData);
		// Change testData pointer to tempData
		testData = &tempData;

#ifdef FUBI_TEMPLATEREC_DEBUG_DRAWING
		if (drawPoints)
			FubiImageProcessing::showPlot(*testData, 640, 480, "resampledInput");
#endif
	}

	Vec3f indicativeOrientDiff(Math::NO_INIT);
	if (m_useOrientations)
	{
		// Get angular difference of first points
		indicativeOrientDiff = testData->front() - trainingData.front();
	}
	else
	{
		// Translate centroid to origin
		translate(*testData, -centroid(*testData));
		// Get angular difference of first points
		indicativeOrientDiff = calculateIndicativeOrient(*testData) - indicativeOrient;
	}
	// Angle should not be more different than the maximum rotation
	if (indicativeOrientDiff.length() < m_maxRotation)
	{
		if (!m_useOrientations)
		{
			// Now remove rotation to keep a little rotation invariance
			rotate(*testData, -indicativeOrientDiff);

#ifdef FUBI_TEMPLATEREC_DEBUG_DRAWING
			if (drawPoints)
			{
				FubiImageProcessing::showPlot(*testData, 640, 480, "rotatedInput");
			}
#endif
		}
		// Scale to unit cube
		// but keep aspect ratio by fitting the largest side to length one
		if (scale(*testData, Vec3f(1.0f, 1.0f, 1.0f), m_minAabbSize, !m_aspectInvariant, !m_aspectInvariant))
		{
			if (m_useDTW)
			{
				// Perform DTW
				// TODO: applying DTW reversely might be better, but this conflicts with the other normalization steps and the indicative angle
				dist = Fubi::calculateDTW(trainingData, *testData, m_distanceMeasure, m_maxWarpingDistance, false, &m_dtwDistanceBuffer, covariances);
				// Normalize by gesture length
				dist /= trainingData.size();
			}
			else
				// Use unwarped pair-wise distance similar to the 1$ recognizer (already includes normalization)
				dist = Fubi::calculateMeanDistance(trainingData, *testData, m_distanceMeasure, covariances);
		}
	}
	return dist;
}
