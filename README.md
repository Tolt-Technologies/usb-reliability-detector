# ToltTech.USBReliabilityDetector

## Description

USB Reliability Detector was created to help diagnose USB device stability issues on Windows computers.  This app takes an 'end to end' approach of observing when devices are removed and added to the device tree, so as to be able to measure the end user impacts of overall usb system instability.

## Installation

XCopy deployment -- e.g. copy the folder onto a computer, no other installation needed.

You can download from:

https://tolttech-bits.s3.us-west-2.amazonaws.com/usb-reliability-detector/2309.0.0.0+Release.zip

or check out the Release page at:

https://gitlab.com/tolttech/utilities/usb-reliability-detector/-/releases/production

## Usage

Run ToltTech.USBReliabilityDetector.exe from the copied folder.

Console app prints results to the console and shows a little console spinner to let you know it is working.

Press any key with the console window in focus to exit the app.

App is logging the results to a local file (path is printed in the first line upon launch) and app is logging to Papertrail (see app logs at https://my.papertrailapp.com/groups/27861981/events?q=program%3AToltTech.USBReliabilityDetector)

## Project status

Project is complete, do not expect to revisit it other than a potential future usage of pulling out the USB detection logic to create a new feature for Ability Drive to "auto inpect" the USB devices to ensure everything is connected and found as expected for a working system.
