# GymTracker

GymTracker is a full-stack fitness tracking web application built to help users manage workouts, discover exercises, create workout plans, log training sessions, and track progress over time.

It uses **ASP.NET Core Web API** for the backend, **Angular** for the frontend, and **PostgreSQL** for data storage. The project also integrates external exercise data from the **wger API**.

---

## Features

- Dashboard overview
- Exercise browsing and search
- Exercise filtering
- Favorite exercises
- Workout plan creation and management
- Workout session logging
- Progress tracking
- Progress visualization with charts
- External exercise import and synchronization
- Development seed data
- REST API communication between frontend and backend

---

## Tech Stack

### Frontend

- Angular
- TypeScript
- HTML
- SCSS
- Chart.js

### Backend

- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Npgsql

### External Integration

- wger Exercise API

---

## Project Structure

```text
GymTracker/
├── backend/
│   └── GymTracker.Api/
├── frontend/
│   └── gymtracker-client/
├── GymTracker.sln
├── README.md
└── .gitignore
```

# How It Works

GymTracker is split into two main parts: the backend API and the frontend client.

## Backend

The backend is built with ASP.NET Core Web API and is responsible for:

handling business logic
connecting to the PostgreSQL database
exposing REST API endpoints
managing exercises, workout plans, workout sessions, favorites, and progress entries
importing and synchronizing external exercise data
seeding initial data for development
## Frontend

The frontend is built with Angular and is responsible for:

displaying the user interface
consuming backend API endpoints
rendering dashboard data, charts, forms, and lists
giving users an interactive experience for managing their fitness information

The frontend and backend communicate through HTTP requests, while PostgreSQL stores the application data.

# Main Modules
### Dashboard

The dashboard provides a quick overview of the application and key activity.

### Exercises

Users can browse, search, and filter exercises, and mark selected exercises as favorites.

### Workout Plans

Users can create structured workout plans and organize exercises into reusable routines.

### Workout Sessions

Users can log completed training sessions and maintain a history of workouts.

### Progress Tracking

Users can record progress data such as body weight and view visual progress over time using charts.

### External Exercise Import

The project supports importing and synchronizing exercise data from the wger API.

### API Overview

The backend exposes REST API endpoints used by the Angular frontend.

# Examples of supported functionality include:

retrieving exercises
filtering and searching exercises
importing exercise data from external APIs
creating and reading workout plans
creating and reading workout sessions
saving progress entries
managing favorite exercises
Getting Started
Prerequisites

# Before running the project, make sure you have installed:

.NET SDK,
Node.js and npm,
Angular CLI,
PostgreSQL,
Git.

#Images:
