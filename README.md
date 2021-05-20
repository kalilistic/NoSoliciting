# NoSoliciting

*Adblock for FFXIV.*

## Summary

NoSoliciting filters chat messages and Party Finder listings based on
various built-in and custom filters. The built-in filters are generated
using data from `NoSoliciting.Trainer/data.csv` to create a machine
learning model. They can be updated without a plugin update. Custom
filters are user-defined and are either case-insensitive substrings or
regular expressions.

All messages and listings filtered can be logged in case of false
positives, and there is a reporting mechanism in which users can
report if NoSoliciting had a false positive or a false negative.
