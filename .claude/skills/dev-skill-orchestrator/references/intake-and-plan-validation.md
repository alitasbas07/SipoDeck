# Intake and plan validation

Accept natural-language task input. The current Orca session identifies the project; do not require the user to repeat the project name.

Resolve plans from an explicit absolute path, project-relative path, or named folder under `.todo`. Read phase files in intended order. Extract a ClickUp or Flowdo ID or URL. When absent, search by supplied task title and ask the user if multiple plausible matches exist.

Compare local plans with the tracker main task and subtasks. Neither source automatically wins. Record matches, omissions and conflicts. A material conflict, missing source, unclear acceptance criterion or unresolved task match requires `WAITING_USER` before development.

