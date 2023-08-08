using CommandLine;
using Kemocade.Vrc.Tracker.Action;
using Kemocade.Vrc.Tracker.Action.Models;
using OtpNet;
using System.Text.Json;
using VRChat.API.Api;
using VRChat.API.Client;
using VRChat.API.Model;
using static System.Console;
using static System.IO.File;
using static System.Text.Json.JsonSerializer;

// Configure Cancellation
using CancellationTokenSource tokenSource = new();
CancelKeyPress += delegate { tokenSource.Cancel(); };

// Configure Inputs
ParserResult<ActionInputs> parser = Parser.Default.ParseArguments<ActionInputs>(args);
if (parser.Errors.ToArray() is { Length: > 0 } errors)
{
    foreach (CommandLine.Error error in errors)
    { WriteLine($"{nameof(error)}: {error.Tag}"); }
    Environment.Exit(2);
    return;
}
ActionInputs inputs = parser.Value;

// Find Local Files
DirectoryInfo workspace = new(inputs.Workspace);
DirectoryInfo output = workspace.CreateSubdirectory(inputs.Output);

// Authentication credentials
Configuration config = new()
{
    Username = inputs.Username,
    Password = inputs.Password,
    UserAgent = "kemocade/0.0.1 admin%40kemocade.com"
};

// Create instances of API's we'll need
AuthenticationApi authApi = new(config);
GroupsApi groupsApi = new(config);
TrackedGroup trackedGroup;

try
{
    // Log in
    WriteLine("Logging in...");
    CurrentUser currentUser = authApi.GetCurrentUser();

    if (currentUser == null)
    {
        WriteLine("2FA needed...");

        // Generate a 2fa code with the stored secret
        string key = inputs.Key.Replace(" ", string.Empty);
        Totp totp = new(Base32Encoding.ToBytes(key));

        // Make sure there's enough time left on the token
        int remainingSeconds = totp.RemainingSeconds();
        if (remainingSeconds < 5)
        {
            WriteLine("Waiting for new token...");
            await Task.Delay(TimeSpan.FromSeconds(remainingSeconds + 1));
        }

        WriteLine("Using 2FA code...");
        authApi.Verify2FA(new(totp.ComputeTotp()));
        currentUser = authApi.GetCurrentUser();

        if (currentUser == null)
        {
            WriteLine("Failed to validate 2FA!");
            Environment.Exit(2);
        }
    }

    WriteLine($"Logged in as {currentUser.DisplayName}");

    // Get group
    string groupId = inputs.Group;
    Group group = groupsApi.GetGroup(groupId);
    int memberCount = group.MemberCount;
    WriteLine($"Got Group {group.Name}, Members: {memberCount}");

    // Get group roles
    WriteLine("Getting Group Roles...");
    GroupRole[] groupRoles = groupsApi
        .GetGroupRoles(groupId)
        .OrderBy(gr => gr.Name)
        .ToArray();
    WriteLine($"Got {groupRoles.Length} Group Roles");

    // Get group members
    WriteLine("Getting Group Members...");
    List<GroupMember> groupMembers = new();

    // Get self and ensure self is in group
    GroupMyMember self = group.MyMember;
    if (self == null)
    {
        WriteLine("User must be a member of the group!");
        Environment.Exit(2);
    }

    // Get non-self group members and add to group members list
    while (groupMembers.Count < memberCount - 1)
    {
        groupMembers.AddRange
            (groupsApi.GetGroupMembers(groupId, 100, groupMembers.Count, 0));
        WriteLine(groupMembers.Count);
        await Task.Delay(1000);
    }

    // Add self to group member list
    groupMembers.Add
    (
        new
        (
            self.Id,
            self.GroupId,
            self.UserId,
            self.IsRepresenting,
            new(currentUser.Id, currentUser.DisplayName),
            self.RoleIds,
            self.JoinedAt,
            self.MembershipStatus,
            self.Visibility,
            self.IsSubscribedToAnnouncements
        )
    );

    groupMembers = groupMembers
        .OrderBy(gm => gm.User.DisplayName)
        .ToList();

    WriteLine($"Got {groupMembers.Count} Group Members");

    trackedGroup = new TrackedGroup
    {
        Roles = groupRoles
            .Select
            (
                gr =>
                new TrackedGroupRole
                {
                    Id = gr.Id,
                    Name = gr.Name,
                    MemberIndexes = groupMembers
                        .Select((gm, i) => (gm, i))
                        .Where(gmi => gmi.gm.RoleIds.Contains(gr.Id))
                        .Select(gmi => gmi.i)
                        .ToArray()
                }
            )
            .ToArray(),
        Members = groupMembers
            .Select
            (
                gm => new TrackedGroupMember
                {
                    Id = gm.UserId,
                    Name = gm.User.DisplayName,
                    RoleIndexes = groupRoles
                        .Select((gr, i) => (gr, i))
                        .Where(gri => gm.RoleIds.Contains(gri.gr.Id))
                        .Select(gri => gri.i)
                        .ToArray()
                }
            )
            .ToArray()
    };
}
catch (ApiException e)
{
    WriteLine("Exception when calling API: {0}", e.Message);
    WriteLine("Status Code: {0}", e.ErrorCode);
    WriteLine(e.ToString());
    Environment.Exit(2);
    return;
}

JsonSerializerOptions jsonSerializerOptions = new()
{ PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

string trackedGroupJson = Serialize(trackedGroup, jsonSerializerOptions);
WriteLine(trackedGroupJson);

FileInfo outputJson = new(Path.Join(output.FullName, "output.json"));
WriteAllText(outputJson.FullName, trackedGroupJson);

WriteLine("Done!");
Environment.Exit(0);