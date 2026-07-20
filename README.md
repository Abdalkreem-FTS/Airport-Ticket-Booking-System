# Airport Ticket Booking System

A .NET 10 console application for booking airport flight tickets, with an interactive
terminal UI powered by Spectre.Console. Passengers can book, search, view, cancel, and
modify flights; managers can bulk-import flights from CSV, filter bookings, and inspect
validation rules.

Data lives in plain JSON files. Rather than reading and overwriting those files naively,
every change runs through a small **ACID transaction engine built on top of the file
system** — with SQL-style isolation levels, locking, MVCC snapshots, a write-ahead log,
and crash recovery. See [ARCHITECTURE.md](ARCHITECTURE.md) for how that works.

## Features

- Book, search, view, cancel, and modify flights for passengers
- Manager tools: import flights from CSV, filter bookings, inspect validation rules

## Prerequisites

- Git
- .NET 10 SDK

## Run

```bash
git clone https://github.com/Abdalkreem-FTS/Airport-Ticket-Booking-System

cd Airport-Ticket-Booking-System

dotnet run --project ATBS.Console
```
