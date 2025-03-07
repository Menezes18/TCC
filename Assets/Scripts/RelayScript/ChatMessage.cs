using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public struct ChatMessage : NetworkMessage
{
    public string content;
}
