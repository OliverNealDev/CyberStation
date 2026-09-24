# Cyber Station

A neon station management game about building a transport hub that can take the pressure: placement, passenger flow, ratings and progression all pulling against each other.

Built solo in Unity 6 as the artefact for my final-year project at Teesside University, where it scored **90/100** and won **Best Games Programming and Development Artefact at ExpoTees 2026**.

[Play in your browser on itch.io](https://olivernealdev.itch.io/cyber-station) · [Full technical breakdown](https://oliverneal.dev/cyber-station.html) · [Portfolio](https://oliverneal.dev)

| | |
|---|---|
| **Role** | Solo developer |
| **Engine** | Unity 6 (6000.3) · C# |
| **Released** | April 2026 |
| **Latest update** | Rush Hour update, September 2026 |
| **Scale** | 109 scripts · ~22,300 lines of C# · 43 EditMode tests |
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

## The Rush Hour update

The graded build had strong systems and a weak first ten minutes. A new player landed on an empty floor with $2,000 and nothing to say that a station needs a Materializer, a Ticket Machine and a train line before a single passenger appears. Once it was running, nothing changed from one minute to the next, and the only feedback was a star rating behind a menu. The update goes after those three gaps: goals, rhythm and feedback.

It was planned from research into what drives reviews in the genre (clear goals, pause and speed controls, being able to see why something happened, disruption that is announced rather than random) and the most common complaints about small management games (nothing to aim for, no challenge, hidden systems, a stream of pop-ups to click away).

- **Directives.** A goal list on the right of the screen. The first four are the tutorial: one at a time, with the top-bar button you need pulsing. After that three run at once with money and XP rewards, and when the authored chain runs out, contracts are generated that scale with tier. Rating goals only count with at least eight passengers inside, because an empty concourse scores nearly five stars.
- **A station clock.** Days with a morning and an evening rush hour and quiet nights, which reshape passenger demand. The demand curve averages just under 1 across a day, so it moves the pressure around without changing what a station earns.
- **Announced events.** Concert crowds, signal faults that hold a platform's trains outside, transit inspections that pay out or fine you, fare-dodger crews, heatwaves, brownouts, influencer streams and tourist coaches, each announced 20 seconds before it hits. None of them adds a branch to the passenger simulation: an event is a row of multipliers in `StationModifiers`, which the existing systems read.
- **End-of-day reports.** At midnight the station pauses on a graded report from S to D: passengers served, needs met, money, the rush-hour rating, what passengers gave up on and why, and the weakest rating with what to do about it. A C or better earns a subsidy.
- **Control and clarity.** Pause and three speeds (Space, 1, 2, 3), a live star rating in the top bar that opens the breakdown, a help card (F1), and a title screen with Continue and New Station. Passengers now say what is wrong above their heads, using the speech bubbles and dialogue data that were already on every passenger but never called.
- **Feel.** The balance rolls towards its new value, placed objects pop in, demolishing shows the refund, and reaching a tier announces what it unlocked. HUD sounds are synthesised at startup rather than shipped as audio files.

How it is built:

- **Nothing in the scene file changed.** The new systems are created at scene load by `GameSystemsBootstrap`, the same pattern `SaveManager` already used, and the HUD is built in code by `UiKit` with the scene's own font and canvas scaler settings, so it lines up with the existing top bar at any resolution.
- **Anything that can be pure is pure.** The clock, day grading, directive rules and the event scheduler live in their own assembly, `CyberStation.Simulation`, which depends on nothing else in the game. That is what makes them testable.
- **One owner for `Time.timeScale`.** `GameSpeedManager` is the only thing that writes it. The title screen and the day report take a pause lock instead of setting the speed, so closing one can never unpause the game underneath another.
- **Versioned saves.** Saves now carry a version number, and new systems register as an `ISaveParticipant` that stores its own section, so adding one no longer means editing `SaveManager`. A save from before the update loads as version 1, and directives the station has clearly already done are marked complete without paying out a second time.

---

## Architecture

109 scripts and roughly 22,300 lines of C#. The first 86, about 17,100 lines, were written by one person over one academic year as the graded artefact; the Rush Hour update added the rest. They are sorted into layers with different jobs rather than a folder of scripts.

| Count | Layer | Responsibility |
|---|---|---|
| 16 | **Managers** | Global state and the simulation tick: passengers, economy, ratings, progression, trains, saves, the grid, and since the update the clock, game speed, stats, directives and events |
| 4 | **Coordinators** | Hand out work, so no worker has to know about any other |
| 47 | **Controllers** | One placeable thing or one piece of interface each: ticket machines, barriers, platforms, vending machines, menus, the HUD |
| 5 | **Characters** | Passenger, Staff, Janitor, Security Drone, on a shared `Person` base |
| 13 | **Components** | Small reusable behaviours: billboarding, hover reveals, progress bars, need icons, tweens |
| 8 | **ScriptableObjects** | Data, not code: buildables, trains, staff, expansions, dialogue and visuals |
| 4 | **Simulation** | Pure logic in its own assembly: the clock, day grading, directive rules, the event catalogue and scheduler |

Control runs downward, data upward, nothing sideways. The split that mattered most was putting **coordinators between managers and characters**: before that, staff behaviour was asking the world about other staff, and every new worker type made it worse. Adding the security drone later was a new character plus a new coordinator rather than an edit to everything that already existed.

The ScriptableObject layer is what kept the original 39 controllers from becoming 200. Trains, staff and buildables are authored as data, so most new content is an asset rather than a class.

---

## What I would change

Written up honestly rather than in general terms, because the repository is public and you can go and confirm all three. This is the list as it stood at release, with what the Rush Hour update did about each.

1. **Eleven managers is too many, and the dependency graph proves it.** `ProgressionManager` is referenced by six of the other ten and reaches back into `EconomyManager` itself, closing a cycle. It happened because a tier unlock is simultaneously a money question and a progression question and I never picked one owner for it. The coordinators layer is the proof I know how to fix it: I did exactly this for the staff and not for progression.
   *Since the update:* the cycle is still there. The five new managers read the old ones' state, but none of the old managers reference them: the simulation raises static events and reads one set of multipliers, so the new systems could be removed without touching it.
2. **The save system knows about everything.** `SaveManager` is 712 lines and touches seven other managers across 27 call sites, so adding a manager means editing the save system too. It had no version field, so a save written in week twelve could not be loaded by the build from week thirty. A version number plus a migration path is a day of work I should have spent early.
   *Since the update:* saves are versioned, and a pre-update save loads and migrates. New systems save themselves through `ISaveParticipant`, so the save system stopped growing, but the original seven managers are still restored by hand.
3. **There were no tests.** Not a single test assembly. The fixed-tick systems are deterministic by construction, which makes them the cheapest things in the codebase to test and the place balance bugs hid longest. Ratings, queueing and the economy, in that order.
   *Since the update:* 43 EditMode tests cover the new simulation assembly. The original ratings, queueing and economy code is still untested, and is still first on the list.

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
