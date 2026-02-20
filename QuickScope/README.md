# QuickScope

QuickScope is a lightweight, instantaneous screen capture tool designed for speed and simplicity.

## Status

This project is currently a prototype. It targets .NET 10.0 on Windows.

## Features

* **Instant Capture**: Uses native GDI+ BitBlt for near-instantaneous screen freezing.
* **Region Selection**: Click and drag to select a specific area of the screen to crop.
* **Automatic Saving**: Screenshots are automatically saved as PNG files to your Pictures/Screenshots folder.
* **Clipboard Integration**: Every capture is automatically copied to the system clipboard for immediate use.

## Usage

1. Run the application.
2. Press `CTRL + SHIFT + S` to trigger a capture.
3. Click and drag your mouse to select an area.
4. Release the mouse button to save the crop.
5. Press `ESCAPE` to save the full screen capture and exit the selection mode.

## Requirements

* Windows 10 or later.
* .NET 10.0 Runtime.