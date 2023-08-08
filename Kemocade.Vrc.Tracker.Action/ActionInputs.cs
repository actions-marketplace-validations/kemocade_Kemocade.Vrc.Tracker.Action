﻿using CommandLine;

namespace Kemocade.Vrc.Tracker.Action;

internal record ActionInputs
{
    [Option('w', "workspace", Required = true)]
    public string Workspace { get; init; } = null!;

    [Option('o', "output", Required = true)]
    public string Output { get; init; } = null!;

    [Option('u', "username", Required = true)]
    public string Username { get; init; } = null!;

    [Option('p', "password", Required = true)]
    public string Password { get; init; } = null!;

    [Option('k', "key", Required = true)]
    public string Key { get; init; } = null!;

    [Option('g', "group", Required = true)]
    public string Group { get; init; } = null!;
}