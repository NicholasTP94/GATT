import { useEffect, useState } from "react";

interface SessionSummary {
  id: string;
  startedAt: string;
}

export function SessionsPage() {
  const [sessions, setSessions] = useState<SessionSummary[]>([]);

  useEffect(() => {
    fetch("/api/sessions")
      .then((response) => response.json())
      .then(setSessions);
  }, []);

  return (
    <main>
      <h1>Telemetry sessions</h1>
      <ul>
        {sessions.map((session) => (
          <li key={session.id}>{session.startedAt}</li>
        ))}
      </ul>
    </main>
  );
}
