-- Adds SQLite expression indexes for score chart lookups.
-- Safe to rerun.

CREATE INDEX IF NOT EXISTS IX_Stats_ZoneId_Score
ON Stats (ZoneId, (AssistPoints + BonusPoints + KillPoints) DESC);

CREATE INDEX IF NOT EXISTS IX_StatsDailies_ZoneId_Score
ON StatsDailies (ZoneId, (AssistPoints + BonusPoints + KillPoints) DESC);

CREATE INDEX IF NOT EXISTS IX_StatsDailies_ZoneId_Date_Score
ON StatsDailies (ZoneId, Date, (AssistPoints + BonusPoints + KillPoints) DESC);

CREATE INDEX IF NOT EXISTS IX_StatsWeeklies_ZoneId_Score
ON StatsWeeklies (ZoneId, (AssistPoints + BonusPoints + KillPoints) DESC);

CREATE INDEX IF NOT EXISTS IX_StatsWeeklies_ZoneId_Date_Score
ON StatsWeeklies (ZoneId, Date, (AssistPoints + BonusPoints + KillPoints) DESC);

CREATE INDEX IF NOT EXISTS IX_StatsMonthlies_ZoneId_Score
ON StatsMonthlies (ZoneId, (AssistPoints + BonusPoints + KillPoints) DESC);

CREATE INDEX IF NOT EXISTS IX_StatsMonthlies_ZoneId_Date_Score
ON StatsMonthlies (ZoneId, Date, (AssistPoints + BonusPoints + KillPoints) DESC);

CREATE INDEX IF NOT EXISTS IX_StatsYearlies_ZoneId_Score
ON StatsYearlies (ZoneId, (AssistPoints + BonusPoints + KillPoints) DESC);

CREATE INDEX IF NOT EXISTS IX_StatsYearlies_ZoneId_Date_Score
ON StatsYearlies (ZoneId, Date, (AssistPoints + BonusPoints + KillPoints) DESC);

ANALYZE;
PRAGMA optimize;
