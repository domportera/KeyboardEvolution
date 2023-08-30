# KeyboardEvolution

This repo is to figure out what the most ergonomic keyboard layout is for [Thumb Key](https://github.com/dessalines/thumb-key) by using some half-assed variation of evolutionary algorithms.

This is built as a command-line executable and should be run in a terminal. The first time it runs, it will generate a settings file you can use to dramatically modify the outputs. The documentation for what these settings mean is currently only [commented as summaries](./src/ThumbKey/TrainerSettings.cs).

The dataset I use can be found at the bottom of [this page](https://affect.media.mit.edu/neural_chat/datasets/). If unzipped and placed next to the executable (as in "./reddit_casual.json"), the application should find it automatically.

Happy hunting :)
