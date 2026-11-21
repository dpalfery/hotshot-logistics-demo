---
id: demo/user-story
title: Demo user story - dashboard job status cards
doc-type: reference
status: current
owner: unassigned
last-reviewed: 2026-11-21
---

Prerequisites
•  The four job statuses — Pending, Assigned, EnRoute and Received — already exist in the platform. This story introduces no new or renamed status.
•  The dashboard overview and the job data it reads are available in the environment where the story is verified.
•  Jobs exist in more than one status in that environment, so a genuine count of zero can be told apart from a count that failed to load.

Description
  As an operations admin, we need to see how many jobs sit in each status on the dashboard overview so that we can tell at a glance where the pipeline is backed up without filtering the jobs list by hand.

  In scope:
•  A count for each of the four job statuses, reflecting the stored job data at the time the overview is displayed. Counts are never hardcoded, mocked, or taken from sample data.
•  A status with no jobs displays a count of zero — not an empty card, and not an error.
•  The four counts are presented together as a group on the dashboard overview, positioned between the existing headline metrics and the recent jobs list.
•  Each card shows the status name, a status icon, and the count.
•  The group uses the same visual language as the existing headline metric cards — the same card size, spacing, corner radius, shadow and type scale — so the overview reads as a single surface.
•  Each status is distinguishable by its icon and colour, per the table below. Exact colour values and the icon set follow the design system the dashboard already uses.
•  Each card shows a loading state until its count is available, and an error state when the count cannot be retrieved. One unavailable count does not prevent the other cards, or the rest of the overview, from displaying.
•  The counts refresh when a job's status changes, without the admin reloading the page.
Status	Colour	Icon
Pending	Blue	Hourglass
Assigned	Cyan	Clipboard
EnRoute	Teal blue	Truck
Received	Azure blue	Inbox
Out of Scope
•  New job statuses, and any change to the existing status names or their meaning.
•  Selecting a card to drill into the jobs in that status.
•  Historical, trend or time-series counts, and any charting.
•  Counts for invoices, drivers or any entity other than jobs.
•  Changes to the existing headline metrics or to the recent jobs list.

Assumptions
•  A job has exactly one status at a time, so the four counts add up to the total number of jobs.
•  The job data behind the counts is current enough to be meaningful at the moment the overview is displayed.
•  The admin viewing the overview is already authorised to see job data, so the counts require no additional access.
•  The counts are read from the same job data the jobs list reads; no separate or duplicated source of job counts is introduced.
 
Business Case: Operations staff see how many jobs sit in each status without filtering the jobs list, so a backlog in one status is visible as soon as the overview loads. It also breaks down the existing Active Jobs metric, which today collapses Pending, Assigned and EnRoute into a single number.
•  Acceptance Criteria:
◦  The dashboard overview displays four job status cards: Pending, Assigned, EnRoute and Received.
◦  Each card shows the count of jobs in that status, and a status with no jobs displays zero.
◦  The four counts match the stored job data at the time the overview is displayed, and add up to the total number of jobs.
◦  No count is hardcoded or sourced from sample data.
◦  The cards appear between the existing headline metrics and the recent jobs list.
◦  The cards match the existing headline metric cards in size, spacing, corner radius, shadow and type scale.
◦  Each status is distinguishable by the icon and colour listed in the Description.
◦  A card shows a loading state before its count is available and an error state when the count cannot be retrieved, and a failed count leaves the other cards and the rest of the overview usable.
◦  When a job's status changes, the counts update without the admin reloading the page.
•  Suggested Tests:
◦  Against a known set of jobs spanning more than one status, verify the four counts match the stored data and add up to the total job count.
◦  Verify a status with no jobs displays zero rather than an empty or failed card.
◦  Verify the overview still renders its other sections when a count cannot be retrieved.
◦  Verify the counts change without a page reload after a job's status changes.
