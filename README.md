# Code Combiner

Code Combiner is a Unity Editor tool designed to combine and copy code files to the clipboard. It allows you to select multiple code files within your project, merge their contents, and copy the merged content to the clipboard for easy use—especially handy for pasting into tools like ChatGPT and other LLMs!

https://github.com/cihanozcelik/CodeCombiner/assets/4025567/8f89244d-10e6-4b73-87f4-ca73af84823f

## Features

- Select multiple code files from your Unity project.
- Combine selected files into a single content.
- Copy the combined content to the clipboard with a single click.
- Adds the file names as comments at the top of each merged file.

## Installation

To install the Code Combiner tool using Unity Package Manager, follow these steps:

1. Open your Unity project.
2. In the menu bar, go to `Window > Package Manager`.
3. Click the `+` button in the top left corner of the Package Manager window.
4. Select `Add package from git URL...`.
5. Enter the following URL and click `Add`:

   ```
   https://github.com/cihanozcelik/CodeCombiner.git
   ```

Alternatively, you can add the package directly to your `manifest.json` file located in the `Packages` folder of your Unity project:

```json
{
  "dependencies": {
    "com.nopnag.codecombiner": "https://github.com/cihanozcelik/CodeCombiner.git"
  }
}
```

## Usage

1. After installing the package, open the Code Combiner tool by going to `Tools > CodeCombiner` in the Unity menu bar.
2. The Code Combiner window will open, displaying a list of folders and files in your project.
3. Navigate through the folders and select the code files you want to combine by checking the boxes next to the file names.
4. The total number of selected files and lines will be displayed at the bottom of the window.
5. Click the `Combine and Copy to Clipboard` button to merge the selected files and copy the combined content to the clipboard.
6. Paste the combined content wherever you need it.

## Note

This tool is not optimized for very large codebases yet. Besides, ChatGPT doesn't accept more than around 4500 lines (100 characters per line cap) in my tests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

Cihan Özçelik

For more information, visit the [repository](https://github.com/cihanozcelik/CodeCombiner).
