# Thesis

This is the repo for my final M.Sc. Thesis *"AI Copycats: Imitation Learning for Driving Style Modeling with Unity ML-Agents"*.

## Overview

This project builds on the [Karting Microgame](https://learn.unity.com/project/karting-template) Unity Template ([Asset Store](https://assetstore.unity.com/packages/templates/unity-learn-karting-microgame-urp-150956)), all tracks were created with the [Racetrack Addon](https://assetstore.unity.com/packages/3d/racetrack-karting-microgame-add-ons-174459) and the models were trained via the [ML-Agents Library](https://github.com/Unity-Technologies/ml-agents).

The aim of this project was to combine these elements in order to model the driving style of a user and create a driving agent that could mimic it.

To this purpose, work was done on:
- User Application: a simple WebGL app consisting of a series of tracks in which user actions are recorded ([Unity Play](https://play.unity.com/en/games/a3a8144d-4125-4184-b532-56d1286fb004/kart-data-collection));
- Firebase server: simple (and free) storage for gathered data;
- Editor Scripts: much needed automation for (mainly) the training and evaluation procedures.

## Contents

This repo contains the Unity project for both the User Application and the processing framework.

Both make use of the **Agent** component, provided by the ML-Agents library:
- When using the *User Application*, it allows the `DemonstrationWriter` component to save all relevant data;
- When *Training*, it manages episodes, collecting observation and rewards, and communicating with the Python part of the library;
- When *Inferencing*, it applies the action obtained by the `DecisionRequester`'s queries to the model, controlling the Agent as an user would.

### User Application

A series of 5 tracks (plus an introductory, free-roam scene).

Completing a lap yields two files:
- The `.demo` file contains data for the training algorithm (user inputs, sensor data, rewards, etc.);
- The `.state` file contains replay data (position, rotation, velocity, angular rotation for each fixed timestep).

### Processing Framework

A set of Editor scripts to automate training and evaluation.

The expected usage of the ML-Agents library entails a great amount of manual interaction with the Unity Editor and the command prompt. This is acceptable if training a few models, but quickly becomes unmanaeageable for a per-user setup like this, or if hyperparameter optimization is to be performed whatsoever.

## Usage

First, set all settings in `Project Settings > Kart Settings`: all "default" settings act as failsafes/auto-completion for training and evaluation, but there are a couple of actual settings (namely, Conda path and Track scene folder) that need to be set before starting.

All Editor Windows are found under the "Kart" tab.
![](/MenuTab.png)

### Import Demos from Firebase

Before training a model (unless training with Reinforcement Learning), user-produced replay files have to be split into training and testing sets. Training files provide demonstrations for Imitation Learning, whereas evaluation files serve as reference when evaluating a trained model.

This utility sends simple `GET` requests to the server (server path structure is hardcoded in [RequestManager.cs](/Assets/My%20Scripts/RESTManager.cs)) and downloads respective files to an arbitrary folder.

There are two download types, "N per Track" and Cumulative, each denoted by a specific folder structure.

N per Track downloads the first N laps from each track as demonstration, and leaves the remaining as replays (in this case there are 4 laps and N = 3):
```
├───demonstrations
│   ├───0
│   │       Track0-0.demo
│   │       Track0-1.demo
│   │       Track0-2.demo
│   │
│   ├───1
│   │       Track1-0.demo
│   │       Track1-1.demo
│   │       Track1-2.demo
│   │
│   ├───2
│   │       Track2-0.demo
│   │       Track2-1.demo
│   │       Track2-2.demo
│   │
│   ├───3
│   │       Track3-0.demo
│   │       Track3-1.demo
│   │       Track3-2.demo
│   │
│   └───4
│           Track4-0.demo
│           Track4-1.demo
│           Track4-2.demo
│
└───replays
    ├───0
    │       Track0-3.state
    ├───1
    │       Track1-3.state
    ├───2
    │       Track2-3.state
    ├───3
    │       Track3-3.state
    └───4
            Track4-3.state
```

Cumulative requires specifying which track to use as demonstration and which as replay. Once specified, all laps are downloaded:
```
├───demonstrations
│   ├───0
│   │       Track0-0.demo
│   │       Track0-1.demo
│   │       Track0-2.demo
│   │       Track0-3.demo
│   │
│   ├───1
│   │       Track1-0.demo
│   │       Track1-1.demo
│   │       Track1-2.demo
│   │       Track1-3.demo
│   │
│   ├───2
│   │       Track2-0.demo
│   │       Track2-1.demo
│   │       Track2-2.demo
│   │       Track2-3.demo
│   │
│   └───4
│           Track4-0.demo
│           Track4-1.demo
│           Track4-2.demo
│           Track4-3.demo
│
└───replays
    └───3
            Track3-0.state
            Track3-1.state
            Track3-2.state
            Track3-3.state
```

### Setup Training

This is the main tool: it manages training the models and evaluating them.

A **Training Session** is defined as a series of **Steps**, each can be a *Training* or *Evaluation* step:
- Training: runs the `mlagents-learn` executable (requires `trainer` and `run-id` parameters) from the specified *conda* environment, but also instances tracks and agents and starts Play Mode;
- Evaluation: takes a previously trained model and evaluates it against a folder of user files.

Extra, one-time fields are for values that are common to all steps:
- Username: it is added as a prefix to every training step's `run-id` field;
- Trainer: it is passed as a parameter to every training step;
- Track Override: repeats the entire *session* for each track of the list by substituting it in every "Track" field. Supports replacing the default ordinal suffix with a custom one (useful if starting from a step that's not the first - e.g. if restarting after a crash -, but supports any string).

### Evaluate Model

Offers the same functionality as an *Evaluation* step, but is somewhat optimized for standalone use and visualization.