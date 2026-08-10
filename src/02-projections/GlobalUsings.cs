// Mirrors src/Shared/DedupeAliases.cs in the Marten repository so the vendored
// TeleHealth files under TeleHealth/ can stay byte-for-byte identical to
// upstream apart from their namespace. IdentityAttribute moved to JasperFx in
// jasperfx#335 / marten#4525.
global using IdentityAttribute = JasperFx.IdentityAttribute;
