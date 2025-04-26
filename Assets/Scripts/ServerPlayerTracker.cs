using UnityEngine;
using System.Collections.Generic;
using System;
public static class ServerPlayerTracker {
    public static HashSet<String> ConnectedClientIds = new();
}