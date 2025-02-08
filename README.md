<div align="center">
  <img src="images/Icon1.png" height="150px" width="auto">
  <h1>TextControlBox-UWP</h1>
</div>

<div align="center">
  <a href="https://www.microsoft.com/store/productId/9NWL9M9JPQ36">
    <img src="https://img.shields.io/badge/Download demo App-Microsoft%20Store-brightgreen?style=flat">
  </a>
  <img src="https://img.shields.io/github/issues/FrozenAssassine/TextControlBox-UWP.svg?style=flat">
  <img src="https://img.shields.io/github/issues-closed/FrozenAssassine/TextControlBox-UWP.svg">
  <img src="https://img.shields.io/github/stars/FrozenAssassine/TextControlBox-UWP.svg">
  <img src="https://img.shields.io/github/repo-size/FrozenAssassine/TextControlBox-UWP">
  
  <br>

  [![NuGet version (TextControlBox)](https://img.shields.io/nuget/v/TextControlBox.JuliusKirsch)](https://www.nuget.org/packages/TextControlBox.JuliusKirsch)
</div>

---

> ⚠️ **This version is deprecated!** Check out the new version for WinUI-3:  
> 👉 [TextControlBox-WinUI](https://github.com/FrozenAssassine/TextControlBox-WinUI)

---

## 🤔 What is TextControlBox?
A UWP-based textbox with syntax highlighting and support for handling very large amounts of text. This project is still in development.

## 🛠 Why I Built It
UWP provides a default `TextBox` and `RichTextBox`, but both perform poorly when rendering thousands of lines. Additionally, selection handling is slow. To solve these issues, I created my own textbox control.

## 📥 Download

<a href="https://www.nuget.org/packages/TextControlBox.JuliusKirsch">
  <img width="40" height="auto" src="https://raw.githubusercontent.com/devicons/devicon/master/icons/nuget/nuget-original.svg"/>
</a>

## 🔍 Features:
- Viewing files with a million lines or more without performance issues
- Syntaxhighlighting
- Outstanding performance because it only renders the lines that are needed to display
- Linenumbering
- Linehighlighter
- Json to create custom syntaxhighlighting
- Highly customizable


## ❗ Problems:
- Current text limit is 100 million characters
- Currently there is no textwrapping
- First version for WinUI with many problems and bugs, but working [Github](https://github.com/FrozenAssassine/TextControlBox-WinUI/)

## 🚩 Available languages:
- Batch
- Config file
- C++
- C#
- CSV
- CSS
- GCode
- Hex
- Html
- Java
- Javascript
- Json
- Markdown
- LaTex
- PHP
- Python
- QSharp
- Toml
- Xml

## 🚀 Usage:

<details><summary><h2>Properties</h2></summary> 
 
 ```
- ScrollBarPosition (get/set)
- CharacterCount (get)
- NumberOfLines (get)
- CurrentLineIndex (get)
- SelectedText (get/set)
- SelectionStart (get/set)
- SelectionLength (get/set)
- ContextFlyoutDisabled (get/set)
- ContextFlyout (get/set)
- CursorSize (get/set)
- ShowLineNumbers (get/set)
- ShowLineHighlighter (get/set)
- ZoomFactor (get/set)
- IsReadonly (get/set)
- Text (get/set)
- RenderedFontSize (get)
- FontSize (get/set)
- FontFamily (get/set)
- Cursorposition (get/set)
- SpaceBetweenLineNumberAndText (get/set)
- LineEnding (get/set)
- SyntaxHighlighting (get/set)
- CodeLanguage (get/set)
- RequestedTheme (get/set)
- Design (get/set)
- static CodeLanguages (get/set) 
- VerticalScrollSensitivity (get/set)
- HorizontalScrollSensitivity (get/set)
- VerticalScroll (get/set)
- HorizontalScroll (get/set)
- CornerRadius (get/set)
- UseSpacesInsteadTabs (get/set)
- NumberOfSpacesForTab (get/set)
  ```
</details>
<details>
  <summary><h2>Functions</h2></summary>
 
  ```
- SelectLine(index)
- GoToLine(index)
- SetText(text)
- LoadText(text)
- Paste()
- Copy()
- Cut()
- GetText()
- SetSelection(start, length)
- SelectAll()
- ClearSelection()
- Undo()
- Redo()
- ScrollLineToCenter(line)
- ScrollOneLineUp()
- ScrollOneLineDown()
- ScrollLineIntoView(line)
- ScrollTopIntoView()
- ScrollBottomIntoView()
- ScrollPageUp()
- ScrollPageDown()
- GetLineContent(line)
- GetLinesContent(startline, count)
- SetLineContent(line, text)
- DeleteLine(line)
- AddLine(position, text)
- FindInText(pattern)
- SurroundSelectionWith(value)
- SurroundSelectionWith(value1, value2)
- DuplicateLine(line)
- FindInText(word, up, matchCase, wholeWord)
- ReplaceInText(word, replaceword, up, matchCase, wholeword)
- ReplaceAll(word, replaceword, up, matchCase, wholeword)
- static GetCodeLanguageFromJson(jsondata)
- static SelectCodeLanguageById(identifier)
- Unload()
- ClearUndoRedoHistory()
  ```
</details>

## Create custom syntaxhighlighting languages with json:
```json
{
  "Highlights": [
    {
      "CodeStyle": { //optional delete when not used
        "Bold": true, 
        "Underlined": true, 
        "Italic": true
      },
      "Pattern": "REGEX PATTERN",
      "ColorDark": "#ffffff", //color in dark theme
      "ColorLight": "#000000" //color in light theme
    },
  ],
  "Name": "NAME",
  "Filter": "EXTENSION1|EXTENSION2", //.cpp|.c
  "Description": "DESCRIPTION",
  "Author": "AUTHOR"
}  
```

### To bind it to the textbox you can use one of these ways:
```cs

TextControlBox textbox = new TextControlBox();

//Use a builtin language -> see list a bit higher
//Language identifiers are case intensitive
textbox.CodeLanguage = TextControlBox.GetCodeLanguageFromId("CSharp");

//Use a custom language:
var result = TextControlBox.GetCodeLanguageFromJson("JSON DATA");
if(result.Succeed)
     textbox.CodeLanguage = result.CodeLanguage; 
```

## Create custom designs in C#:
```cs
textbox.Design = new TextControlBoxDesign(
    new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)), //Background brush
    Color.FromArgb(255, 255, 255, 255), //Text color
    Color.FromArgb(100, 0, 100, 255), //Selection color
    Color.FromArgb(255, 255, 255, 255), //Cursor color
    Color.FromArgb(50, 100, 100, 100), //Linehighlighter color
    Color.FromArgb(255, 100, 100, 100), //Linenumber color
    Color.FromArgb(0, 0, 0, 0), //Linenumber background
    Color.FromArgb(100,255,150,0) //Search highlight color
    );
```


## 👨‍👩‍👧‍👦 Contributors:
If you want to contribute for this project, feel free to open an <a href="https://github.com/FrozenAssassine/TextControlBox-UWP/issues/new">issue</a> or a <a href="https://github.com/FrozenAssassine/TextControlBox-UWP/pulls">pull request</a>.

## 📸 Images

<img src="images/image1.png">
