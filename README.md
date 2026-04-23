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

#Preview

<img width="1905" height="901" alt="image" src="https://github.com/user-attachments/assets/3114d4da-25b2-4775-9c68-05e693ffd7b3" />
<img width="1910" height="904" alt="image" src="https://github.com/user-attachments/assets/4d732f26-c573-4fdf-93d0-5a6170cb2ccf" />
<img width="1916" height="900" alt="image" src="https://github.com/user-attachments/assets/110c95d1-312d-4d2e-af92-31e369e95530" />
<img width="1907" height="912" alt="image" src="https://github.com/user-attachments/assets/bc9f1e57-d390-46b0-8658-a18741cd86c3" />
<img width="1918" height="906" alt="image" src="https://github.com/user-attachments/assets/def469bb-d2f9-4a64-8980-da940f800a11" />
<img width="1919" height="905" alt="image" src="https://github.com/user-attachments/assets/9b691915-7760-4521-a230-bc828b18866c" />
<img width="1919" height="911" alt="image" src="https://github.com/user-attachments/assets/acca5ca0-504a-48cc-96b8-87cfae9aa5d6" />
<img width="1917" height="914" alt="image" src="https://github.com/user-attachments/assets/e826328f-010e-4750-99ce-8999c292a463" />
<img width="1919" height="905" alt="image" src="https://github.com/user-attachments/assets/10a33718-9a60-477a-afea-88badca16c6d" />
<img width="1919" height="916" alt="image" src="https://github.com/user-attachments/assets/a647ec54-60db-47fc-b6fe-fc7e6004aecc" />
<img width="1916" height="910" alt="image" src="https://github.com/user-attachments/assets/c9aa5eb6-9de0-48de-8b6f-b034751c8bf5" />
<img width="1907" height="909" alt="image" src="https://github.com/user-attachments/assets/1e069726-c054-49af-804a-7a92f97aaeeb" />






