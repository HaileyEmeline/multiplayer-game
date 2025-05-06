using UnityEngine;
using System.Collections.Generic;
using System;

//Stores the list of connected clients to the server
public static class ServerPlayerTracker {
    public static HashSet<String> ConnectedClientIds = new();
}