# TextControlBox-UWP
An UWP based textbox with Syntaxhighliting and support for very large amount of text


## Reason why I built it
UWP has a default Textbox and a RichTextBox. Both of them are very slow in rendering multiple thounsand lines. The selection works also very slow. So I decided to create my own version of a Textbox.

## Features:
- Open a file with a million lines or more without laggs
- Syntaxhighlighting with customisation and api to create your own Regex patterns for it
- Outstanding performance because it only renders as many lines as your screen can display

## Problems:
- Because all the lines are stored in a List the ram usage with a million lines ore more is very high.
- Performance of deleting a long range of text is slow, because it needs to remove all the lines from the list

#### The Control is not done yet and should not be used in production. There are many features missing and not everything works without crashing sometimes. I will do my very best to make it done soon.


<img src="images/image1.png">
