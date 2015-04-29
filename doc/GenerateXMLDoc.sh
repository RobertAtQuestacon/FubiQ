#!/bin/bash
xsddoc -o FubiXML -t "Fubi XML Documentation" -css "./XMLdoc.css" -cf "../bin/FubiRecognizers.xsd"
xdg-open FubiXML/index.html
