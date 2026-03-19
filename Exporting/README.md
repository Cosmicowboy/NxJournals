# NX Component STEP Exporter
## Overview

This project contains a C# NX Open journal that automates the process of exporting selected assembly components to individual STEP files. It scans an assembly, identifies components that match a specific naming pattern, groups them, presents them in a [custom selection UI](https://github.com/Cosmicowboy/NxJournals/blob/main/Forms/CustomFormBuilder.cs), and exports their bodies using NX’s StepCreator.

The script is intended for workflows where components follow a standardized naming convention, and users need to export multiple parts quickly and consistently.
Key Features

    Automatic component discovery  
    Recursively traverses the assembly tree and identifies components that match the naming pattern:

    ^\d{5}(-[A-Z]|-\d{2})?_(\d{3}|[xX]{3})_.+

    Name normalization  
    Ensures Component.Name matches Component.DisplayName (case‑insensitive), updating it if needed.

    Grouped component selection UI  
    Displays a Windows Form with a multi-select list of all matched components.

    User‑selected export directory  
    Prompts the user to choose an output folder before exporting.

    STEP export automation  
    Uses NX’s StepCreator to export bodies from selected components:

        AP214 format

        Solids, curves, and coordinate systems

        Layer mask filtering

        External reference support

    Body extraction from prototypes  
    Converts component handles into actual NX body objects suitable for STEP export.

How It Works
1. Component Discovery

The script begins at the assembly root and recursively walks through all children:

    Filters components using CheckIfMatchedComponent()

    Normalizes names with CheckNamesMatch()

    Groups components by their (normalized) name in a dictionary

2. User Interaction

Two dialogs are shown:

    FolderBrowserDialog – user selects the export directory

    Custom Form – user selects which grouped components to export

The form is built using the included MyFormBuilder class.
3. Export Process

For each selected component group:

    Extracts bodies using GetNxBodies()

    Builds a ComponentDTO (not shown in this file but referenced)

    Exports bodies to a STEP file using ExportBodies()

If a component’s number is "XXX", the script skips database write‑back (if integrated externally).
Important Classes & Methods
TraverseComponents()

Recursively walks the assembly and populates the component dictionary.
CheckIfMatchedComponent()

Validates components using a regex pattern to enforce naming standards.
CreateNxExporter()

Configures the NX StepCreator with export settings.
ExportBodies()

Handles STEP export and error reporting.
GetNxBodies()

Maps component instances to their prototype bodies, excluding subtract layers.
MyFormBuilder

A lightweight form builder for creating NX‑compatible Windows Forms without additional licensing.
Usage Instructions

    Open the assembly in NX.

    Run the journal.

    Choose an output folder when prompted.

    Select the components you want to export from the list.

    Click Accept to begin exporting.

    STEP files will be written to the selected directory.

Requirements

    Siemens NX with .NET API support

    C# compiler or NX Journal environment

    Windows Forms support (standard in NX journal execution)

Room For Modifications

    Requires components to follow the naming pattern; unmatched components are ignored. Update to match your naming convention

    Only exports bodies not on the SubtractBodies layer(can be modified to export for any layer 1-256).

    Does not export suppressed or empty-reference-set components (visible compoennts only can be modified to fit).

Potential Enhancements

    Add progress bar or status window.

    Support exporting multiple formats (e.g., Parasolid, JT).

    Add filtering by layer, reference set, or component type.

    Add logging to external file instead of NX Info window.
