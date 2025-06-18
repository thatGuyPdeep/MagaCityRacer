# MagaCityRacer - Unity DOTS ECS Racing Game

A high-performance racing game built with Unity's DOTS (Data-Oriented Technology Stack) ECS (Entity Component System) framework, optimized for mobile devices.

## Project Overview

This project transforms Unity's Megacity DOTS template into a mobile racing game featuring:
- High-performance ECS architecture for smooth mobile gameplay
- Procedural city environments adapted for racing tracks
- Mobile-optimized controls and UI
- Performance-focused design for various mobile devices

## Setup Instructions

### Prerequisites
- Unity 2023.3 LTS or later
- Unity DOTS packages
- Android/iOS build modules

### Project Setup
1. Create a new Unity project using the DOTS template
2. Import the Megacity sample from Package Manager
3. Configure mobile platform settings
4. Set up racing game systems and components

## Architecture

### Core Systems
- **VehicleMovementSystem**: Handles car physics and movement
- **TrackGenerationSystem**: Procedural track generation using city infrastructure
- **CameraFollowSystem**: Dynamic camera system for racing
- **InputSystem**: Mobile touch controls
- **UISystem**: Racing HUD and menus
- **AudioSystem**: Engine sounds and music

### Key Components
- **VehicleComponent**: Car properties (speed, handling, etc.)
- **TrackComponent**: Track waypoints and boundaries
- **PlayerInputComponent**: Touch input data
- **CameraTargetComponent**: Camera following behavior

## Mobile Optimization

### Performance Features
- LOD (Level of Detail) system for city buildings
- Culling optimizations for mobile GPUs
- Texture streaming for memory management
- Physics optimization for mobile processors

### Controls
- Touch steering with tilt option
- Acceleration/brake buttons
- Adaptive UI for different screen sizes

## Development Roadmap

### Phase 1: Core Racing Mechanics
- [ ] Vehicle physics implementation
- [ ] Basic track generation
- [ ] Mobile input system
- [ ] Camera follow system

### Phase 2: Game Systems
- [ ] Lap tracking and timing
- [ ] AI opponents using ECS
- [ ] Power-ups and boost system
- [ ] Mobile UI implementation

### Phase 3: Content & Polish
- [ ] Multiple car models
- [ ] Track variety and themes
- [ ] Audio implementation
- [ ] Performance optimization

### Phase 4: Mobile Features
- [ ] Touch controls refinement
- [ ] Battery optimization
- [ ] Platform-specific features
- [ ] Analytics integration

## Building for Mobile

### Android
```bash
# Set up Android build settings
- Minimum API Level: 24 (Android 7.0)
- Target API Level: Latest
- Scripting Backend: IL2CPP
- Architecture: ARM64
```

### iOS
```bash
# Set up iOS build settings
- Minimum iOS Version: 12.0
- Architecture: ARM64
- Metal API Validation: Disabled for performance
```

## Performance Guidelines

- Keep entity counts reasonable for mobile hardware
- Use burst compilation for critical systems
- Implement proper memory management
- Profile regularly on target devices

## Contributing

Please follow Unity's DOTS best practices and mobile optimization guidelines when contributing to this project. 