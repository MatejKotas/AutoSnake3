# AutoSnake3

Algorithm to play the game Snake automatically. Uses similar approach to [this repository](https://github.com/BrianHaidet/AlphaPhoenix/tree/master/Snake_AI_(2020a)_DHCR_with_strategy).
For more background information, see [this youtube video](https://www.youtube.com/watch?v=TOpBcfbAgPg).

## Algorithm Summary

The algorithm runs at the start of the game and every time an apple is eaten, as opposed to every move the snake makes. On initialization, a default Hamiltonian Cycle is generated (`InitilizeAlgorithm()`). Then, every time the algorithm is run, the existing Hamiltonian Cycle is strategically edited to make the path to the apple as short as possible. Unlike the algorithm described in the YouTube video, this algorithm considers the path to the apple as a whole. This makes the looping issue impossible.

## Performace

| Mean     | Standard Deviation | Minimum | Median | Maximum |
| -------- | ------------------ | ------- | ------ | ------- |
| 47308.97 | 1919.8             | 41476   | 47254  | 55726   |

Performance characteristics over 1000 simulated 30 x 30 games. All values are moves made by the snake per game.
