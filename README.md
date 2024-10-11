# 🌟 Clood Plugin for IntelliJ IDEA 🚀

## 🎉 Welcome to Clood! 🎊

Clood is a game-changing plugin for IntelliJ IDEA that supercharges ⚡ your development workflow. Get ready to
revolutionize the way you code!

> **🚨 IMPORTANT: Alpha Release 🚨**
>
> Clood is currently in **ALPHA**. While it's packed with awesome features, you might encounter some bugs 🐛. We're
> working hard to squash them!
>
> 📅 **Coming Soon**: We're planning to release Clood on the JetBrains Plugin Store this Sunday, after some final
> testing. Stay tuned!

## 🛠️ Prerequisites

- IntelliJ IDEA Ultimate (latest version) 💻

## 🏗️ Building the Plugin

Let's build this bad boy! 🏋️‍♂️

1. Fire up IntelliJ IDEA Ultimate 🔥
2. Peek into the Gradle tool window (View > Tool Windows > Gradle) 👀
3. Hunt down the `jar` task in the Gradle tasks list 🕵️‍♂️
4. Double-click that `jar` like you mean it! 👆👆
5. Watch the magic happen as Gradle creates your plugin JAR ✨
6. Find your shiny new JAR in the `build/libs` folder 💎

## 🔌 Installing the Plugin

Time to plug it in! 🔌

1. Launch IntelliJ IDEA (the one you want to bedazzle with Clood) 🚀
2. Navigate to `File > Settings` (on macOS, it's `IntelliJ IDEA > Preferences`) ⚙️
3. Find your way to `Plugins` in the left sidebar 🧭
4. Click the gear icon ⚙️ (top right) and choose "Install Plugin from Disk" 💽
5. Hunt down that JAR file in your `build/libs` folder 🕵️‍♀️
6. Smash that "OK" button and restart your IDE! 🔄

## 🎬 Setting Up Clood

Before the show begins, let's set the stage:

1. In your project's root directory, conjure up a new folder named `clood-groups` 📁✨
2. This magical folder will be the home of all your Clood group configurations 🏠

Without this folder, Clood will be sad 😢 (and won't work properly).

## 👆 Features

### 🗂️ Clood Groups

- Organize your files and prompts like a boss 😎
- Access your Clood groups from the swanky Clood panel in the IDE 🎛️
- Groups hang out in the `clood-groups` folder you crafted earlier 🏠

### 📄 Clood Files

- Whip up `.clood` files to edit and manage your prompts 📝
- Work on your Clood prompts outside the panel - freedom! 🕊️
- Remember to update the prompt in the Clood panel after editing (there's a speedy action for this) ⚡

### ➕ Add to Clood Action

- Right-click on a file in the Project Explorer (it's like magic!) 🖱️
- Go to "Tools" and choose "Add to Clood" 🧰
- Watch as your file joins the Clood party! 🎉

### 📤 Send to Clood Actions

- Another right-click adventure in the Project Explorer 🖱️
- Find these treasures under "Tools": 💎
    - "Copy To Active Prompt": Your file, now in Clood-vision, er the current clood group! 👁️
    - "Send to Clood Prompt Helper": Your personal Clood assistant to help you write a prompt. Will replace the current
      clood file's contents.  🦮
    - "Add to Clood":  Adds the file(s) to the current group ❤️

### ⚡ Auto-complete Features

When you're in a `.clood` file, try these magic spells:

- `$`: Summons recent files AND auto-adds them to your Clood group! 🧙‍♂️
- `#`: Calls forth files from your current Clood group 📚
- `~`: Conjures project symbols for your Clood file 🔮

## 🆘 Troubleshooting

- If Clood throws a tantrum, try: `File > Invalidate Caches / Restart` 🔄
- Double-check that `clood-groups` folder - it's crucial! 🔍

## 🆘 Support

Stuck? Questions? We're here for you! 🦸‍♀️🦸‍♂️

- Open an issue on our GitHub repo 🐙
- Or give our support team a shout! 📣

## 🎭 Final Words

Thank you for joining the Clood revolution! 🚀 Remember, we're in alpha, so your feedback is golden 🏅. Help us make Clood
even more awesome!

Now go forth and code with the power of Clood! ⚡🖥️⚡
