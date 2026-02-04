#!/bin/bash

# Build script for QuickConsumeWidget iOS Widget Extension
# This script builds the widget extension for both device and simulator

set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_DIR="$SCRIPT_DIR/QuickConsumeWidget.xcodeproj"
BUILD_DIR="$SCRIPT_DIR/XReleases"

echo "=== Building QuickConsumeWidget iOS Widget Extension ==="
echo "Script directory: $SCRIPT_DIR"
echo "Build directory: $BUILD_DIR"

# Clean previous builds
echo "Cleaning previous builds..."
rm -rf "$BUILD_DIR"

# Check if Xcode project exists
if [ ! -d "$PROJECT_DIR" ]; then
    echo "Error: Xcode project not found at $PROJECT_DIR"
    echo "Please create the Xcode project first using Xcode."
    exit 1
fi

# Build for iOS device
echo "Building for iOS device..."
xcodebuild -project "$PROJECT_DIR" \
    -scheme "QuickConsumeWidget" \
    -configuration Release \
    -sdk iphoneos \
    BUILD_DIR="$BUILD_DIR" \
    clean build

# Build for iOS simulator
echo "Building for iOS simulator..."
xcodebuild -project "$PROJECT_DIR" \
    -scheme "QuickConsumeWidget" \
    -configuration Release \
    -sdk iphonesimulator \
    BUILD_DIR="$BUILD_DIR" \
    clean build

echo "=== Build Complete ==="
echo "Device build: $BUILD_DIR/Release-iphoneos/QuickConsumeWidgetExtension.appex"
echo "Simulator build: $BUILD_DIR/Release-iphonesimulator/QuickConsumeWidgetExtension.appex"
