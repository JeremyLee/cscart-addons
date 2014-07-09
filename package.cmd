@echo off

git describe --abbrev=0 --tags > tag
set /p tag=<tag
del tag

git archive --format zip --output releases/%tag%.zip %tag%
