# Cyber Station

A neon station management game about building a transport hub that can take the pressure: placement, passenger flow, ratings and progression all pulling against each other.

Built solo in Unity 6 as the artefact for my final-year project at Teesside University, where it scored **90/100** and won **Best Games Programming and Development Artefact at ExpoTees 2026**.

[Play in your browser on itch.io](https://olivernealdev.itch.io/cyber-station) · [Full technical breakdown](https://oliverneal.dev/cyber-station.html) · [Portfolio](https://oliverneal.dev)

| | |
|---|---|
| **Role** | Solo developer |
| **Engine** | Unity 6 (6000.3) · C# |
| **Released** | April 2026 |
| **Scale** | 86 scripts · ~17,100 lines of C# |
| **Grade** | 90 / 100 |

> The repository is named `TSA` for historical reasons. It is Cyber Station.

---

## Overview

You design and operate a growing neon transport hub: lay out the concourse, keep crowds of passengers moving, and keep your star rating high enough to unlock the next tier. Four systems constantly push against each other:

- **Placement**, which decides how the space is shaped
- **Passenger flow**, which reacts to that shape
- **Station ratings**, which grade the result live
- **Progression**, which gates what you can build next

Every layout decision changes how passengers path through the space, which changes the ratings, which gates what you can build next. Around that core sit train scheduling, staff coordination, an economy, and saving and loading so a station survives between sessions.

---

## Key systems

### Passenger flow on a fixed 10 Hz tick

Passengers are autonomous agents: they enter, navigate to ticket machines, queue, pass barriers and reach their platform in time for the next train, hundreds of them at once. Rather than every passenger running a per-frame update, the whole crowd is driven from a single manager on a **fixed 10 Hz logic tick**.

Each passenger runs a two-level state machine: a master state for where it is in its journey (`InStation`, `OnPlatform`, `OnTrain`) and a sub-state for what it is doing there (`Idle`, `MovingToTarget`, `InteractingWithSomething`). Splitting it that way means "walking somewhere" is written once and reused, instead of once per place a passenger can be walking.

### Routing by lowest estimated total delay

When a passenger needs a facility it does not walk to the nearest one. It picks the option with the **lowest estimated total delay**, and both halves of that estimate are in the same unit. The queue wait is already seconds; the walk is turned into seconds by measuring the real NavMesh path corner to corner and dividing by that passenger's own speed. Adding them is then meaningful, where adding a distance to a queue length would not be, and it is why a nearer machine with six people at it correctly loses to a further one standing empty.

Two details in `PassengerManager` are deliberate:

- A **single reused `NavMeshPath`** for every query rather than one per candidate, because this runs for every passenger that wants something and allocating per candidate would hand the garbage collector a steady drip of work.
- When no complete path exists, the estimate **falls back to flat distance rather than infinity**, so an unreachable facility sorts last instead of vanishing. That is what stops a passenger freezing when the player walls something off mid-journey.

### Paying for the NavMesh bake only when the floor changes

`BuildNavMesh()` is a synchronous full rebuild on the main thread, so rebaking per placement would hitch the game every time you put down a bin. So there are two mechanisms, split by what actually changed:

- Placing a ticket machine or barrier does not change the walkable floor, it just puts something on it, so all 22 buildable prefabs carry a `NavMeshObstacle` and **carve** the existing mesh at runtime for free.
- The mesh is only genuinely rebuilt when the floor itself grows, which is when the player buys a station expansion, so the cost lands on a deliberate, occasional action instead of on every click.

Passengers and dropped litter are colliders too, and a bake would carve people-shaped holes into the mesh they are about to walk on, so they are switched off for the duration and restored inside a `try`/`finally`. Each helper records the **previous** enabled state rather than blanket-enabling everything afterwards, so anything already disabled for its own reasons stays disabled.

### The station rating system

The station is graded live. An overall star rating is derived from six separate ratings (Cleanliness, Crowdedness, Queue Lengths, Service, Decoration and Choice) alongside a passenger throughput percentage. Each watches a different behaviour, so a station can be profitable and still bleed stars because people are bunching at a pinch point.

The loop closes: the six ratings are smoothed and rolled into an overall score, and that score **feeds back into how many passengers spawn**. A better station pulls in bigger crowds, which puts more pressure on the very layout that earned the rating.

Ratings run on a one second tick and are eased toward their target with `Mathf.Lerp` rather than snapped, because raw targets jump around as passengers move and a flickering star rating reads as a bug rather than as feedback. Because the tick is fixed at 1 Hz, `Mathf.Lerp(current, target, 0.2f)` is exponential smoothing on a fixed step, not the frame-rate dependent per-frame lerp it resembles: the same station settles at the same speed on a 30 FPS laptop and a 240 Hz monitor.

### Staff that dispatch themselves

Janitors and security drones do not each hunt for work independently, which would leave two janitors racing to the same piece of litter. Each type has a **coordinator**: workers ask it for a job, and the coordinator hands out the nearest unclaimed task and locks it, so nothing is ever double-assigned. Security prioritises chasing fare evaders over routine patrols through the same dispatch model.

### Progression and expansion

Income and ratings feed a tier system that paces the game. Higher tiers unlock station expansions (more physical space), extra platforms (capacity for more lines), new train lines (the actual service) and staff (service and cleanliness pressure valves), each one raising passenger volume and putting fresh pressure back on the layout.

---

## Architecture

86 scripts and roughly 17,100 lines of C#, written by one person over one academic year, sorted into layers with different jobs rather than a folder of scripts.

| Count | Layer | Responsibility |
|---|---|---|
| 11 | **Managers** | Global state and the simulation tick: passengers, economy, ratings, progression, trains, saves, the grid |
| 3 | **Coordinators** | Hand out work, so no worker has to know about any other |
| 39 | **Controllers** | One placeable thing each: ticket machines, barriers, platforms, vending machines, menus |
| 5 | **Characters** | Passenger, Staff, Janitor, Security Drone, on a shared `Person` base |
| 11 | **Components** | Small reusable behaviours: billboarding, hover reveals, progress bars, need icons |
| 8 | **ScriptableObjects** | Data, not code: buildables, trains, staff, expansions, dialogue and visuals |

Control runs downward, data upward, nothing sideways. The split that mattered most was putting **coordinators between managers and characters**: before that, staff behaviour was asking the world about other staff, and every new worker type made it worse. Adding the security drone later was a new character plus a new coordinator rather than an edit to everything that already existed.

The ScriptableObject layer is what kept the 39 controllers from becoming 200. Trains, staff and buildables are authored as data, so most new content is an asset rather than a class.

---

## What I would change

Written up honestly rather than in general terms, because the repository is public and you can go and confirm all three.

1. **Eleven managers is too many, and the dependency graph proves it.** `ProgressionManager` is referenced by six of the other ten and reaches back into `EconomyManager` itself, closing a cycle. It happened because a tier unlock is simultaneously a money question and a progression question and I never picked one owner for it. The coordinators layer is the proof I know how to fix it: I did exactly this for the staff and not for progression.
2. **The save system knows about everything.** `SaveManager` is 712 lines and touches seven other managers across 27 call sites, so adding a manager means editing the save system too. It has no version field, so a save written in week twelve cannot be loaded by the build from week thirty. A version number plus a migration path is a day of work I should have spent early.
3. **There are no tests.** Not a single test assembly. The fixed-tick systems are deterministic by construction, which makes them the cheapest things in the codebase to test and the place balance bugs hid longest. Ratings, queueing and the economy, in that order.

---

## Running it

```bash
git clone https://github.com/OliverNealDev/CyberStation.git
```

Open the project in **Unity 6 (6000.3.8f1 or newer)** and load the main scene from `Assets/Scenes`. No external services or API keys are required.

Or skip the editor entirely and [play it in your browser on itch.io](https://olivernealdev.itch.io/cyber-station). A Windows build is on the same page if you would rather run it natively.

The web build is a Unity WebGL target with settings chosen for itch.io's hosting, which cannot be configured server side. [Docs/webgl-itch-deployment.md](Docs/webgl-itch-deployment.md) covers what those settings are and why each one is what it is, along with the build and packaging scripts in `Assets/Editor` and `Tools`.

---

## Author

**Oliver Neal**, gameplay programmer specialising in Unity and C#.

[oliverneal.dev](https://oliverneal.dev) · [itch.io](https://olivernealdev.itch.io) · [LinkedIn](https://www.linkedin.com/in/oliverjackneal/) · [GitHub](https://github.com/OliverNealDev)
