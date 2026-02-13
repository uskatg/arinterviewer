# 👔 AR Interview Coach:Content will be modified afterwards 

**An immersive Mixed Reality application built with Unity and the Meta XR SDK designed to simulate job interviews in your physical environment.**

## 📖 Overview

**AR Interview Coach** leverages the Meta Presence Platform (Passthrough API) to spawn a virtual interviewer directly into your real-world space. The goal is to reduce interview anxiety and improve communication skills through realistic exposure therapy and real-time feedback.

Unlike standard VR, this **Mixed Reality** approach keeps the user grounded in their physical environment while interacting with digital assets.

## ✨ Key Features

* **Mixed Reality Passthrough:** Blends the virtual interviewer into your real world using Meta Quest cameras.
* **Virtual Recruiter:** 3D avatar with basic lip-sync and idle animations.
* **Eye Contact Tracking:** Detects if the user maintains gaze with the interviewer.
* **Question Bank:** Randomized technical and behavioral interview questions.

## 🛠 Tech Stack

* **Engine:** Unity 6000.3.0f1
* **SDK:** Meta XR All-in-One SDK
* **Hardware:** Meta Quest 3
* **Language:** C#

## 🚀 Getting Started

### Prerequisites

* Unity Hub and Unity Unity 6000.3.0f1
* Android Build Support (OpenJDK, Android SDK/NDK)
* Meta Quest Headset (Developer Mode enabled)

### Installation

1.  **Clone the repo:**
    ```bash
    git clone https://github.com/uskatg/arinterviewer.git
    ```
2.  **Open in Unity:**
    Add the project folder to Unity Hub and open it.
3.  **Build Settings:**
    * Ensure **OpenXR** or **Oculus** plugin is enabled in `Project Settings > XR Plug-in Management`.

## 📂 Project Structure

```text
Assets/
├── _Project/
│   ├── Scripts/       # Core logic (Eye tracking, Game flow)
│   ├── Prefabs/       # Interviewer Avatar, UI Panels
│   └── Scenes/        # Main Mixed Reality Scene
└── Oculus/            # Meta SDK