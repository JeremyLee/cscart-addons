@echo off
setlocal enabledelayedexpansion

git describe --abbrev=0 --tags > temp
set /p tag=<temp

git describe --abbrev=0 --tags %tag%~^1 > temp
set /p prevTag=<temp

if defined prevTag (
  git diff --name-only %prevTag% %tag% > temp

  (set files=)
  for /f "delims=" %%x in (temp) do call set files=%%files%% %%x
)

del temp


git archive --format zip --output releases/%tag%.zip %tag%

if defined prevTag (
  git archive --format zip --output releases/%prevTag%-%tag%.zip %tag% %files%
)
