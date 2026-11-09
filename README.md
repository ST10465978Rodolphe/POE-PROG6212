# RaceDay

## Project Description

RaceDay is a full-stack event management system for the South African road running, walking
and cycling community. Event Organisers create and manage events, categories and results,
while Participants browse events, enter them by choosing a category, and track their own
race history.

The project is built in three parts:

| Part | Content | Status |
|------|---------|--------|
| Part 1 | ERD, API endpoint plan, SQL script | Complete |
| Part 2 | RESTful ASP.NET Core API, unit tests, CI/CD | Complete |
| Part 3 | MVC web application, Azure Blob Storage, Docker | Complete |

## User Roles

### Organiser
- Registers and logs in with the role Organiser.
- Sees a dashboard with their events, total enrolments and upcoming dates.
- Creates, edits and deletes **their own** events, with an optional banner image.
- Defines categories (age groups or distances) for their own events.
- Views the enrolments of their own events and confirms or cancels them.
- Records finish times and finishing positions, which are published straight away.
- Views and updates their own profile.

### Participant
- Registers and logs in with the role Participant.
- Browses upcoming events, with search and type filters, and opens the full event details.
- Enters an event by choosing a category.
- Sees every event they entered with a colour-coded enrolment status.
- Sees their personal race history, for example "47th out of 312 finishers".
- Views and updates their own profile and uploads a profile picture.

## Part 1

Planning documents are in the [`docs`](docs) folder:
- ERD: `docs/RaceDay_ERD.drawio.pdf`
- API endpoint plan: `docs/RaceDay API Endpoint Plan.pdf`
- SQL script: `docs/RaceDay_Database.sql`
- Part 1 CI screenshot: `docs/ci-success.PNG`

Part 1 video: (https://youtu.be/--MKCmhBOJ8)

## Part 2: RESTful API

### Technologies Used
- ASP.NET Core Web API (C#)
- Entity Framework Core (Code First) with SQL Server / LocalDB
- Server-side session authentication and PBKDF2 password hashing
- Swagger (Swashbuckle) with annotations
- xUnit, `WebApplicationFactory` and the EF Core InMemory provider for tests

### Authentication
- `POST /api/auth/register` validates the data, checks the role is `Organiser` or
  `Participant`, checks the email is unique, hashes the password with PBKDF2 and saves the user.
- `POST /api/auth/login` verifies the password and stores the `UserId` and `Role` in a
  **server-side session**.
- `POST /api/auth/logout` clears the session.
- The `[RequireRole]` filter protects endpoints: **401** if the user is not logged in,
  **403** if the role is wrong. Organisers can only change or view data of events they own.

### API Endpoints

| Area | Endpoint | Access |
|------|----------|--------|
| Auth | `POST /api/auth/register` | Public |
| Auth | `POST /api/auth/login` | Public |
| Auth | `POST /api/auth/logout` | Public |
| Profile | `GET /api/users/me` | Both roles |
| Profile | `PUT /api/users/me` | Both roles |
| Profile | `POST /api/users/me/profile-picture` | Both roles (Part 3) |
| Events | `GET /api/events` | Both roles |
| Events | `GET /api/events/{id}` | Both roles |
| Events | `GET /api/events/mine` | Organiser |
| Events | `POST /api/events` | Organiser |
| Events | `PUT /api/events/{id}` | Organiser (owner) |
| Events | `DELETE /api/events/{id}` | Organiser (owner) |
| Events | `POST /api/events/{id}/banner` | Organiser (owner) (Part 3) |
| Categories | `GET /api/events/{eventId}/categories` | Both roles |
| Categories | `POST /api/events/{eventId}/categories` | Organiser (owner) |
| Categories | `PUT /api/categories/{id}` | Organiser (owner) |
| Categories | `DELETE /api/categories/{id}` | Organiser (owner) |
| Enrolments | `POST /api/enrolments` | Participant |
| Enrolments | `GET /api/enrolments/mine` | Participant |
| Enrolments | `GET /api/enrolments/event/{eventId}` | Organiser (owner) |
| Enrolments | `PUT /api/enrolments/{id}/status` | Organiser (owner) |
| Results | `POST /api/results` | Organiser (owner) |
| Results | `GET /api/results/mine` | Participant |
| Results | `GET /api/results/event/{eventId}` | Organiser (owner) |

All endpoints are described and testable in Swagger at `/swagger`.

#### Differences from the Part 1 plan
- The profile endpoints are `/api/users/me` (as in my Part 1 plan) rather than `/api/profile/me`.
- `Distance` was added to the `Event` table because the brief requires a distance per event.
- Enrolment status has two values, `Confirmed` and `Cancelled`, matching the Part 1 SQL script.
- The entity for the `Route` table is called `EventRoute` in C# to avoid a clash with
  ASP.NET's own `Route` class.
- Deleting an event or category that already has enrolments returns **409 Conflict**.
- A Participant can hold one active enrolment per event. A cancelled enrolment is
  reactivated if the Participant enters the same category again.
- Part 3 added `BannerUrl` (Event) and `ProfilePictureUrl` (User) columns, the two upload
  endpoints, and `EventDate` and `TotalFinishers` in the result responses.

### Unit Testing
The `RaceDay.Api.Tests` project starts the real API in memory with an in-memory database, so
the tests never touch SQL Server. They cover registration, login, event management,
categories, role enforcement (401 and 403), enrolments and results. Run them with
**Test > Run All Tests** in Visual Studio, or:
```bash
dotnet test RaceDay/RaceDay.slnx
```

Part 2 video: (https://www.youtube.com/watch?v=A3KobKJcdcM)

## Part 3: MVC Web Application, Azure Blob Storage and Docker

### Overview
The MVC web application is the front end of RaceDay. It has **no database access and no
business rules of its own**: every read and write goes through the Part 2 API. Image uploads
also go through the API, which stores the files in Azure Blob Storage. The web application
never talks to Azure directly.

### Technologies Used
- ASP.NET Core MVC (C#), Razor views, Bootstrap 5 and a custom RaceDay theme
- Server-side session for the logged-in user and role
- Azure Blob Storage (used by the API) for event banners and profile pictures
- Docker (multi-stage `Dockerfile`)
- GitHub Actions for CI/CD

### Project Structure

```
POE-PROG6212-part-1
|-- .github
|   `-- workflows
|       |-- part1-ci.yml            Part 1: validates the /docs folder
|       `-- ci-part2.yml            Builds the API and the MVC site, runs tests, builds the Docker image
|-- docs                            Part 1 planning documents and CI screenshots
|-- RaceDay
|   |-- RaceDay.slnx                Visual Studio solution (API, Contracts, Web, Tests)
|   |-- RaceDay.Api                 Part 2 API (+ Azure Blob Storage service in Part 3)
|   |-- RaceDay.Contracts           DTOs shared by the API and the web application
|   |-- RaceDay.Api.Tests           xUnit tests
|   `-- RaceDay.Web                 Part 3 MVC web application
|       |-- Controllers             Account, Home, Organiser, Categories, Enrolments,
|       |                           Results, Participant, Profile
|       |-- Filters                 RequireLoginAttribute (login and role protection)
|       |-- Models                  View models
|       |-- Services                ApiClient (the only class that calls the API), session helpers
|       |-- Views                   Razor pages, layout and shared partials
|       |-- wwwroot                 CSS (RaceDay theme), JavaScript, libraries
|       `-- Dockerfile              Image definition for the web application
|-- .gitignore
`-- README.md
```

### How the Web Application Works
- **Authentication:** registration and login are sent to the API. After a successful login
  the web application stores the user id, role and name in its own session, and keeps the
  API's session cookie so every later call to the API is made as that user.
- **Role-based navigation:** the menu is different for each role. The `[RequireLogin]` filter
  sends anonymous users to the login page and users with the wrong role to an
  "access denied" page, even if they type the address by hand. The API enforces the same
  rules again.
- **Logout:** ends the session in the API and clears the web session, then redirects to login.
- **Errors:** invalid input shows friendly messages, API errors are shown as readable
  messages, and unknown addresses or unexpected errors show a branded error page.
- **Design:** one stylesheet (`wwwroot/css/site.css`) defines the colours and fonts, so all
  pages look the same. Enrolment statuses are colour coded (green Confirmed, red Cancelled).

### Pages

| Role | Page | What it does |
|------|------|--------------|
| Public | Home | Landing page with links to register and log in |
| Public | Register, Log in | Account creation (choose Organiser or Participant) and login |
| Organiser | Dashboard | Events created, upcoming events, total enrolments, list of events |
| Organiser | My Events | Create, edit and delete events, upload a banner image |
| Organiser | Categories | Add, edit and delete the categories of an event |
| Organiser | Enrolments | See who entered an event, with category and status; confirm or cancel |
| Organiser | Results | Record finish time and position for each confirmed participant |
| Participant | Browse Events | Upcoming events with search and type filters |
| Participant | Event details | Banner, description, categories and prices; enter the event |
| Participant | My Events | Upcoming and past enrolments with colour-coded status |
| Participant | My Results | Race history with finish time and position out of finishers |
| Both | My Profile | Update details and upload a profile picture |

### Azure Blob Storage
- The API stores uploaded images in an Azure Blob Storage container and saves only the
  image URL in the database (`Event.BannerUrl`, `AppUser.ProfilePictureUrl`).
- The web application sends the file to the API (`POST /api/events/{id}/banner` and
  `POST /api/users/me/profile-picture`). The API checks the file (JPG, PNG or WebP, up to
  5 MB), uploads it to Azure and returns the URL. The browser then shows the image from
  that URL. Replacing an image removes the old file from Azure.
- **The connection string is a secret and is never committed to GitHub.**

To run the project you need your own Azure Storage account:
1. In the Azure portal create a **Storage account**.
2. Create a container named `racedayimages` with the public access level **Blob**
   (anonymous read access for blobs only).
3. Copy the connection string from **Access keys**.
4. Store it as a user secret (from the repository root):
   ```bash
   dotnet user-secrets set "AzureStorage:ConnectionString" "<your connection string>" --project RaceDay/RaceDay.Api
   dotnet user-secrets set "AzureStorage:ContainerName" "racedayimages" --project RaceDay/RaceDay.Api
   ```
   In Visual Studio you can also right-click `RaceDay.Api` and choose **Manage User Secrets**.

Without these settings everything works except image uploads, which show an error message.

### Setup and Run Locally
Prerequisites: Visual Studio 2022 or newer with the ASP.NET workload (includes SQL Server
LocalDB), and the .NET SDK that matches the projects.

1. **Database:** in the Package Manager Console, with `RaceDay.Api` as the default project, run
   `Update-Database`. This creates `RaceDayApiDb` from the migrations.
2. **Azure:** configure the user secrets as described above.
3. **API address:** the web application reads the API address from
   `RaceDay/RaceDay.Web/appsettings.json` (`Api:BaseUrl`). It must match the HTTPS address of
   the API in `RaceDay/RaceDay.Api/Properties/launchSettings.json`.
4. **Run:** in Visual Studio choose **Configure Startup Projects**, select **Multiple startup
   projects** and start both `RaceDay.Api` and `RaceDay.Web`. Or in two terminals:
   ```bash
   dotnet run --project RaceDay/RaceDay.Api
   dotnet run --project RaceDay/RaceDay.Web
   ```
5. Open the web application at the address shown by `RaceDay.Web`, and Swagger at `/swagger` on the API.

### Run with Docker
The MVC web application runs inside a Docker container. The API keeps running on your
computer (it needs your database and Azure settings), and the container calls it through
the special name `host.docker.internal`.

Docker Desktop must be running. From the repository root:

```bash
# Terminal 1: start the API so the container can reach it
dotnet run --project RaceDay/RaceDay.Api --urls http://0.0.0.0:5123

# Terminal 2: build the image, then start the web application with a single command
docker build -f RaceDay/RaceDay.Web/Dockerfile -t raceday-web RaceDay
docker run -d -p 8080:8080 -e Api__BaseUrl=http://host.docker.internal:5123/ --name raceday-web raceday-web
```

Then open **http://localhost:8080**. The containerised application behaves exactly like the
one running locally.

Useful commands:
```bash
docker ps                      # is the container running?
docker logs raceday-web        # application messages
docker stop raceday-web        # stop it
docker rm raceday-web          # remove it (needed before running it again with the same name)
```
After changing the code, rebuild the image (`docker build ...`) and start a new container.

How the `Dockerfile` works (best practices):
- **Multi-stage build:** the first stage uses the .NET SDK image to restore, build and publish;
  the final stage uses the much smaller ASP.NET runtime image and only copies the published files.
- **Layer caching:** the project files are copied and restored first, so packages are not
  downloaded again unless a project file changes.
- A `Dockerfile.dockerignore` keeps `bin`, `obj` and other local files out of the image.
- The site listens on port 8080, and the API address is set with the `Api__BaseUrl`
  environment variable instead of being written in the image.
- The `DOTNET_VERSION` build argument must match the `TargetFramework` of the projects.

### CI/CD
The workflow `.github/workflows/ci-part2.yml` runs on every push to `main`. It checks the
repository structure, restores packages, builds the solution (the API **and** the MVC web
application), runs all unit tests and builds the Docker image of the web application.

### GitHub Actions Screenshot

![CI/CD green build](docs/ci-success-part3.png)

### Video Presentation

Unlisted YouTube video demonstrating the full running application, the MVC structure, the
Azure Blob Storage integration and the Docker setup:

[Insert your Part 3 YouTube link here]

### Design Decisions and Limitations
- The web application contains no database code. Showing the right data to the right role
  is done twice on purpose: by the menu and `[RequireLogin]` in the web application, and by
  `[RequireRole]` and ownership checks in the API.
- Filters on the Browse Events page only change what is displayed from the list the API returns.
- A recorded result cannot be edited, because the API has no endpoint to change a result.
- Entry fees are shown but online payment is outside the scope of this project.
