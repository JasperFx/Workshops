#!/bin/bash
# Runs once on first Postgres container start. CritterWatch keeps its own
# database; everything else in the workshop shares "workshop".
set -e
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-SQL
    CREATE DATABASE critterwatch;
SQL
