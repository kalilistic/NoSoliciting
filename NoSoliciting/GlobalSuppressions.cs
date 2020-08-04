// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Scope = "type", Target = "~T:NoSoliciting.PFPacket")]
[assembly: SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Scope = "type", Target = "~T:NoSoliciting.PFListing")]
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Scope = "member", Target = "~F:NoSoliciting.PFPacket.padding1")]
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Scope = "member", Target = "~F:NoSoliciting.PFPacket.listings")]
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Scope = "member", Target = "~F:NoSoliciting.PFListing.header")]
[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Scope = "module")]
[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "no", Scope = "module")]
[assembly: SuppressMessage("Design", "CA1034:Nested types should not be visible", Scope = "module")]
[assembly: SuppressMessage("Design", "CA1724", Scope = "module")]
