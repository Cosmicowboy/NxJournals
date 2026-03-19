# MyFormBuilder

MyFormBuilder is a fluent, chainable builder for constructing Windows Forms programmatically. It is designed for situations where the user is limited to licensing restrictions in Nx.

By shifting UI creation into a builder pattern, you can construct forms dynamically, consistently, and without relying on designer‑generated .resx files or licensed design‑time components.

## Why a Builder Pattern?

Many users dont have an authoring license to Nx and cannot leverage teh block styler (Nx form builder). This is a basic way to make modular reusable components in your journals for custom logic and execution.
Windows Forms traditionally relies on the Visual Studio Designer to generate UI code. In environments like Nx, where the designer cannot run or where licensing prevents design‑time features from loading, you lose:

    Drag‑and‑drop UI creation

    Automatic layout generation

    Designer‑generated partial classes

    Visual previews of forms

The builder pattern replaces these design‑time features with a runtime construction pipeline that is:

    Declarative

    Repeatable

    Testable

    Free of licensing requirements

Instead of relying on designer files, the builder constructs the form entirely in code, ensuring compatibility with environments that cannot load the designer, and runtime usage of Windows Forms is not restricted; you can:

    Instantiate controls

    Set properties

    Add them to a form

    Handle events

    Display the form

The builder pattern leverages this by:

    Constructing the UI entirely through runtime code

    Eliminating the need for designer‑generated files

    Ensuring the form is built in a predictable, fluent sequence

This makes it ideal for Nx, CI environments, or any scenario where the designer is unavailable.
Features

    Enables reusable custom forms

    Fluent, chainable configuration methods
    
    Add single or multiple controls

    Remove controls from the collection

    Automatically add Accept/Cancel buttons with wired events

    Auto‑size behavior when no dimensions are specified

    Fully compatible with environments lacking WinForms designer support

    Returns a fully constructed Form via Build()
