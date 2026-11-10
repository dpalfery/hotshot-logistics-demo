---
id: demo/full-prompt
title: Demo full-stack prompt
doc-type: reference
status: current
owner: unassigned
last-reviewed: 2026-11-09
---

# Demo-full-prompt.md

**Unified Prompt — Full-Stack Update for Hotshot Logistics Admin Dashboard**

## Steps

Update the **Hotshot Logistics Admin Dashboard UI** and backend to include **four new job status cards**, maintaining design consistency and enabling end-to-end data flow.

---

### Current Dashboard Metrics

* Total Jobs
* Active Jobs
* Active Drivers
* Overdue Invoices

---

### New Component Placement

* Render the new **Job Status Cards** component between the main metric cubes and the “Recent Activity” section.
* Ensure backend services, APIs, and data models fully support the new status fields from database → API → frontend.

---

### New Status Cards

| Status   | Color                | Icon          |
| :------- | :------------------- | :------------ |
| Pending  | #3B82F6 (Blue)       | Hourglass     |
| Assigned | #0EA5E9 (Cyan)       | ClipboardList |
| EnRoute  | #0284C7 (Teal Blue)  | Truck         |
| Received | #0369A1 (Azure Blue) | Inbox         |

---

### UI Design & Layout

* Same padding, shadows, and rounded corners as existing metric cards.
* Typography: Inter; **semi-bold** labels, **bold** counts.
* Responsive grid: `grid grid-cols-5 gap-4`.
* Each card displays:

  * Top label (e.g., “Pending”)
  * Rounded square badge containing the icon, positioned to the left of the label
  * Count centered below

---

### Frontend Implementation

* Framework: **Next.js 16 (App Router)** using **React + TailwindCSS**.
* Create a reusable component:

  ```tsx
  // JobStatusCards.tsx
  interface JobStatus {
    label: string
    count: number
    color: string
    icon: string
  }
  ```
* Import and render inside the Admin Dashboard page.
* Do not put Mock data in produiction components.
* Ensure design supports live updates when SignalR integration is added.

---

### Backend & Data Integration

* Extend backend models and DTOs to include new job status fields.
* Update service layer and repository logic to aggregate counts per status.
* Ensure Repository layer supports calling the information from the database
* Expose a REST or SignalR endpoint returning job status summaries.
* Confirm data integrity across persistence, API, and UI layers.

---

### Testing & Validation

* Validate that the database values are making it all the way from teh data base to the display. Only do the resting required to support that 
* Verify visual consistency, API connectivity, and responsive layout.
* Ensure test data persists independently from production datasets.
