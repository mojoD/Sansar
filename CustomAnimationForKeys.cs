/* This content is licensed under the terms of the Creative Commons Attribution 4.0 International License.
 * When using this content, you must:
 * �    Acknowledge that the content is from the Sansar Knowledge Base.
 * �    Include our copyright notice: "� 2017 Linden Research, Inc."
 * �    Indicate that the content is licensed under the Creative Commons Attribution-Share Alike 4.0 International License.
 * �    Include the URL for, or link to, the license summary at https://creativecommons.org/licenses/by-sa/4.0/deed.hi (and, if possible, to the complete license terms at https://creativecommons.org/licenses/by-sa/4.0/legalcode.
 * For example:
 * "This work uses content from the Sansar Knowledge Base. � 2017 Linden Research, Inc. Licensed under the Creative Commons Attribution 4.0 International License (license summary available at https://creativecommons.org/licenses/by/4.0/ and complete license terms available at https://creativecommons.org/licenses/by/4.0/legalcode)."
 */

using Sansar.Script;
using Sansar.Simulation;
using Sansar;
using System;
using System.Linq;
using System.Collections.Generic;

public class CustomAnimationForKeys : SceneObjectScript
{
    #region EditorProperties
    // Start playing on these events. Can be a comma separated list of event names.
    public string AnimationClip1 = null;
    public string AnimationClip2 = null;
    public string AnimationClip3 = null;
    public string AnimationClip4 = null;
    public string AnimationClip5 = null;
    public string AnimationClip6 = null;
    public string AnimationClip7 = null;
    public string AnimationClip8 = null;
    public string AnimationClip9 = null;
    public string AnimationClip10 = null;
    public string AnimationClip11 = null;
    public string AnimationClip12 = null;
    public string AnimationClip13 = null;
    public string AnimationClip14 = null;
    public string AnimationClip15 = null;
    public string AnimationClip16 = null;
    public string AnimationClip17 = null;
    public string AnimationClip18 = null;
    public string EnableEvent = null;
    public string DisableEvent = null;

    #endregion

    #region SimpleHelpers v2
    // Update the region tag above by incrementing the version when updating anything in the region.

    // If a Group is set, will only respond and send to other SimpleScripts with the same Group tag set.
    // Does NOT accept CSV lists of groups.
    // To send or receive events to/from a specific group from outside that group prepend the group name with a > to the event name
    // my_group>on
    [DefaultValue("")]
    [DisplayName("Group")]
    public string Group = "";

    public interface ISimpleData
    {
        AgentInfo AgentInfo { get; }
        ObjectId ObjectId { get; }
        ObjectId SourceObjectId { get; }

        // Extra data
        Reflective ExtraData { get; }
    }

    public class SimpleData : Reflective, ISimpleData
    {
        public SimpleData(ScriptBase script) { ExtraData = script; }
        public AgentInfo AgentInfo { get; set; }
        public ObjectId ObjectId { get; set; }
        public ObjectId SourceObjectId { get; set; }

        public Reflective ExtraData { get; }
    }

    public interface IDebugger { bool DebugSimple { get; } }
    private bool __debugInitialized = false;
    private bool __SimpleDebugging = false;
    private string __SimpleTag = "";

    private string GenerateEventName(string eventName)
    {
        eventName = eventName.Trim();
        if (eventName.EndsWith("@"))
        {
            // Special case on@ to send the event globally (the null group) by sending w/o the @.
            return eventName.Substring(0, eventName.Length - 1);
        }
        else if (Group == "" || eventName.Contains("@"))
        {
            // No group was set or already targeting a specific group as is.
            return eventName;
        }
        else
        {
            // Append the group
            return $"{eventName}@{Group}";
        }
    }

    private void SetupSimple()
    {
        __debugInitialized = true;
        __SimpleTag = GetType().Name + " [S:" + Script.ID.ToString() + " O:" + ObjectPrivate.ObjectId.ToString() + "]";
        Wait(TimeSpan.FromSeconds(1));
        IDebugger debugger = ScenePrivate.FindReflective<IDebugger>("SimpleDebugger").FirstOrDefault();
        if (debugger != null) __SimpleDebugging = debugger.DebugSimple;
    }

    System.Collections.Generic.Dictionary<string, Func<string, Action<ScriptEventData>, Action>> __subscribeActions = new System.Collections.Generic.Dictionary<string, Func<string, Action<ScriptEventData>, Action>>();
    private Action SubscribeToAll(string csv, Action<ScriptEventData> callback)
    {
        if (!__debugInitialized) SetupSimple();
        if (string.IsNullOrWhiteSpace(csv)) return null;

        Func<string, Action<ScriptEventData>, Action> subscribeAction;
        if (__subscribeActions.TryGetValue(csv, out subscribeAction))
        {
            return subscribeAction(csv, callback);
        }

        // Simple case.
        if (!csv.Contains(">>"))
        {
            __subscribeActions[csv] = SubscribeToAllInternal;
            return SubscribeToAllInternal(csv, callback);
        }

        // Chaining
        __subscribeActions[csv] = (_csv, _callback) =>
        {
            System.Collections.Generic.List<string> chainedCommands = new System.Collections.Generic.List<string>(csv.Split(new string[] { ">>" }, StringSplitOptions.RemoveEmptyEntries));

            string initial = chainedCommands[0];
            chainedCommands.RemoveAt(0);
            chainedCommands.Add(initial);

            Action unsub = null;
            Action<ScriptEventData> wrappedCallback = null;
            wrappedCallback = (data) =>
            {
                string first = chainedCommands[0];
                chainedCommands.RemoveAt(0);
                chainedCommands.Add(first);
                if (unsub != null) unsub();
                unsub = SubscribeToAllInternal(first, wrappedCallback);
                Log.Write(LogLevel.Info, "CHAIN Subscribing to " + first);
                _callback(data);
            };

            unsub = SubscribeToAllInternal(initial, wrappedCallback);
            return unsub;
        };

        return __subscribeActions[csv](csv, callback);
    }

    private Action SubscribeToAllInternal(string csv, Action<ScriptEventData> callback)
    {
        Action unsubscribes = null;
        string[] events = csv.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (__SimpleDebugging)
        {
            Log.Write(LogLevel.Info, __SimpleTag, "Subscribing to " + events.Length + " events: " + string.Join(", ", events));
        }
        Action<ScriptEventData> wrappedCallback = callback;

        foreach (string eventName in events)
        {
            if (__SimpleDebugging)
            {
                var sub = SubscribeToScriptEvent(GenerateEventName(eventName), (ScriptEventData data) =>
                {
                    Log.Write(LogLevel.Info, __SimpleTag, "Received event " + GenerateEventName(eventName));
                    wrappedCallback(data);
                });
                unsubscribes += sub.Unsubscribe;
            }
            else
            {
                var sub = SubscribeToScriptEvent(GenerateEventName(eventName), wrappedCallback);
                unsubscribes += sub.Unsubscribe;
            }
        }
        return unsubscribes;
    }

    System.Collections.Generic.Dictionary<string, Action<string, Reflective>> __sendActions = new System.Collections.Generic.Dictionary<string, Action<string, Reflective>>();
    private void SendToAll(string csv, Reflective data)
    {
        if (!__debugInitialized) SetupSimple();
        if (string.IsNullOrWhiteSpace(csv)) return;

        Action<string, Reflective> sendAction;
        if (__sendActions.TryGetValue(csv, out sendAction))
        {
            sendAction(csv, data);
            return;
        }

        // Simple case.
        if (!csv.Contains(">>"))
        {
            __sendActions[csv] = SendToAllInternal;
            SendToAllInternal(csv, data);
            return;
        }

        // Chaining
        System.Collections.Generic.List<string> chainedCommands = new System.Collections.Generic.List<string>(csv.Split(new string[] { ">>" }, StringSplitOptions.RemoveEmptyEntries));
        __sendActions[csv] = (_csv, _data) =>
        {
            string first = chainedCommands[0];
            chainedCommands.RemoveAt(0);
            chainedCommands.Add(first);

            Log.Write(LogLevel.Info, "CHAIN Sending to " + first);
            SendToAllInternal(first, _data);
        };
        __sendActions[csv](csv, data);
    }

    private void SendToAllInternal(string csv, Reflective data)
    {
        if (string.IsNullOrWhiteSpace(csv)) return;
        string[] events = csv.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        if (__SimpleDebugging) Log.Write(LogLevel.Info, __SimpleTag, "Sending " + events.Length + " events: " + string.Join(", ", events) + (Group != "" ? (" to group " + Group) : ""));
        foreach (string eventName in events)
        {
            PostScriptEvent(GenerateEventName(eventName), data);
        }
    }
    #endregion

    private Animation animation = null;
    private AnimationParameters initialAnimationParameters;

    private bool killAnimation = false;
    private string[] AnimationEvent = new string[18];
    private string[] AnimationDoneEvent = new string[18];
    private string[] startFrame = new string[18];
    private string[] endFrame = new string[18];
    private string[] PlaybackType = new string[18];
    private string[] AnimationSpeed = new string[18];
    private string[] BlendDuration = new string[18];
    private AnimationComponent animComponent;

    public interface ISendKeyInfo
    {
        string iChannelOut { get; }
        string iKeySent { get; }
    }

    private void getKeyInfo(ScriptEventData gotKeyInfo)
    {

        if (gotKeyInfo.Data == null)
        {
            return;
        }

        ISendKeyInfo sendKeyInfo = gotKeyInfo.Data.AsInterface<ISendKeyInfo>();
        if (sendKeyInfo == null)
        {
            Log.Write(LogLevel.Error, Script.ID.ToString(), "Unable to create interface, check logs for missing member(s)");
            return;
        }
        //Log.Write("CustomAnimatedKeys: In getKeyInfo KeySent: " + sendKeyInfo.iKeySent);
        ExecuteAnimation(sendKeyInfo.iKeySent);
            
    }

    public override void Init()
    {

        if (!ObjectPrivate.TryGetFirstComponent<AnimationComponent>(out animComponent))
        {
            Log.Write(LogLevel.Error, "NPCAnimation.Init", "Object must have an animation added at edit time for NPCAnimation to work");
            return;
        }

        animation = animComponent.DefaultAnimation;
        animation.JumpToFrame(0);
        initialAnimationParameters = animation.GetParameters();
        //SubscribeToAll("Kill", KillAnimation);
        SubscribeToScriptEvent("KeySent", getKeyInfo);

        if (EnableEvent != "")
        {
            Log.Write("Enable Event was not null: " + EnableEvent);
            ParseAll();
            //SubscribeToAll(EnableEvent, Subscribe);
        }
        else
        {
            ParseAll();
            //Subscribe(null);  //executes it by passing null data

        }

        if (DisableEvent != "")
        {
            SubscribeToAll(DisableEvent, Unsubscribe);
        }

    }

    private void ParseAll()  //doesn't really pass data.  Always passes null
    {
        //Look At Animation Strings and subscribe to events
        Log.Write("In Subscribe");

        if (AnimationClip1.Length > 0) ParseAnimation(0, AnimationClip1);
        if (AnimationClip2.Length > 0) ParseAnimation(1, AnimationClip2);
        if (AnimationClip3.Length > 0) ParseAnimation(2, AnimationClip3);
        if (AnimationClip4.Length > 0) ParseAnimation(3, AnimationClip4);
        if (AnimationClip5.Length > 0) ParseAnimation(4, AnimationClip5);
        if (AnimationClip6.Length > 0) ParseAnimation(5, AnimationClip6);
        if (AnimationClip7.Length > 0) ParseAnimation(6, AnimationClip7);
        if (AnimationClip8.Length > 0) ParseAnimation(7, AnimationClip8);
        if (AnimationClip9.Length > 0) ParseAnimation(8, AnimationClip9);
        if (AnimationClip10.Length > 0) ParseAnimation(9, AnimationClip10);
        if (AnimationClip11.Length > 0) ParseAnimation(10, AnimationClip11);
        if (AnimationClip12.Length > 0) ParseAnimation(11, AnimationClip12);
        if (AnimationClip13.Length > 0) ParseAnimation(12, AnimationClip13);
        if (AnimationClip14.Length > 0) ParseAnimation(13, AnimationClip14);
        if (AnimationClip15.Length > 0) ParseAnimation(14, AnimationClip15);
        if (AnimationClip16.Length > 0) ParseAnimation(15, AnimationClip16);
        if (AnimationClip17.Length > 0) ParseAnimation(16, AnimationClip17);
        if (AnimationClip18.Length > 0) ParseAnimation(17, AnimationClip18);
    }

    private void Unsubscribe(ScriptEventData sed)
    {

    }

    private void ParseAnimation(int AnimationNumber, string AnimationIn)
    {
        //Log.Write("In ParseAnimation AnimationNumber: " + AnimationNumber + "  AnimationIn: " + AnimationIn);
        List<string> AnimationArray = new List<string>();
        AnimationArray.Clear();
        AnimationArray = AnimationIn.Split(',').ToList();
        AnimationEvent[AnimationNumber] = AnimationArray[0];
        AnimationDoneEvent[AnimationNumber] = AnimationArray[1];
        //Log.Write("Animation Event: " + AnimationEvent[AnimationNumber]);
        startFrame[AnimationNumber] = AnimationArray[2];
        endFrame[AnimationNumber] = AnimationArray[3];
        PlaybackType[AnimationNumber] = AnimationArray[4];
        AnimationSpeed[AnimationNumber] = AnimationArray[5];
        BlendDuration[AnimationNumber] = AnimationArray[6];

        //PlayAnimationEvent(AnimationNumber);
        
        //SubscribeToAll(AnimationEvent[AnimationNumber], ExecuteAnimation);
        
        //Log.Write("Finished Parse Animation");
    }

    //private void KillAnimation(ScriptEventData data)
    //{
    //    killAnimation = true;
    //}


    private void ExecuteAnimation(string KeySent)
    {
        //Log.Write("In Execute Animation data message: " + data.Message);

        //Log.Write("Animation Event: " + AnimationEvent[0]);
        //string DoneEvent = AnimationDoneEvent[0];
        if (KeySent == AnimationEvent[0])
        {
            PlayAnimationEvent(0);
            if (AnimationDoneEvent[0] != "none")
            {
                PlayAnimationEvent(1);
            }
        }
        else if (KeySent == AnimationEvent[1])
        {
            PlayAnimationEvent(1);
            if (AnimationDoneEvent[1] != "none")
            {
                PlayAnimationEvent(2);
            }
        }
        else if (KeySent == AnimationEvent[2])
        {
            PlayAnimationEvent(2);
            if (AnimationDoneEvent[2] != "none")
            {
                PlayAnimationEvent(3);
            }
        }
        else if (KeySent == AnimationEvent[3])
        {
            PlayAnimationEvent(3);
            if (AnimationDoneEvent[3] != "none")
            {
                PlayAnimationEvent(4);
            }
        }
        else if (KeySent == AnimationEvent[4])
        {
            PlayAnimationEvent(4);
            if (AnimationDoneEvent[4] != "none")
            {
                PlayAnimationEvent(5);
            }
        }
        else if (KeySent == AnimationEvent[5])
        {
            PlayAnimationEvent(5);
            if (AnimationDoneEvent[5] != "none")
            {
                PlayAnimationEvent(6);
            }
        }
        else if (KeySent == AnimationEvent[6])
        {
            PlayAnimationEvent(6);
            if (AnimationDoneEvent[6] != "none")
            {
                PlayAnimationEvent(7);
            }
        }
        else if (KeySent == AnimationEvent[7])
        {
            PlayAnimationEvent(7);
            if (AnimationDoneEvent[7] != "none")
            {
                PlayAnimationEvent(8);
            }
        }
        else if (KeySent == AnimationEvent[8])
        {
            PlayAnimationEvent(8);
            if (AnimationDoneEvent[8] != "none")
            {
                PlayAnimationEvent(9);
            }
        }
        else if (KeySent == AnimationEvent[9])
        {
            PlayAnimationEvent(9);
            if (AnimationDoneEvent[9] != "none")
            {
                PlayAnimationEvent(10);
            }
        }
        else if (KeySent == AnimationEvent[10])
        {
            PlayAnimationEvent(10);
            if (AnimationDoneEvent[10] != "none")
            {
                PlayAnimationEvent(11);
            }
        }
        else if (KeySent == AnimationEvent[11])
        {
            PlayAnimationEvent(11);
            if (AnimationDoneEvent[11] != "none")
            {
                PlayAnimationEvent(12);
            }
        }
        else if (KeySent == AnimationEvent[12])
        {
            PlayAnimationEvent(12);
            if (AnimationDoneEvent[12] != "none")
            {
                PlayAnimationEvent(13);
            }
        }
        else if (KeySent == AnimationEvent[13])
        {
            PlayAnimationEvent(13);
            if (AnimationDoneEvent[13] != "none")
            {
                PlayAnimationEvent(14);
            }
        }
        else if (KeySent == AnimationEvent[14])
        {
            PlayAnimationEvent(14);
            if (AnimationDoneEvent[14] != "none")
            {
                PlayAnimationEvent(15);
            }
        }
        else if (KeySent == AnimationEvent[15])
        {
            PlayAnimationEvent(15);
            if (AnimationDoneEvent[15] != "none")
            {
                PlayAnimationEvent(16);
            }
        }
        else if (KeySent == AnimationEvent[16])
        {
            PlayAnimationEvent(16);
            if (AnimationDoneEvent[16] != "none")
            {
                PlayAnimationEvent(17);
            }
        }
    }

    private void PlayAnimationEvent(int AnimationNumber)
    {

        //Log.Write("Playing Animation Number: " + AnimationNumber + "  Animation: " + AnimationEvent[AnimationNumber]);
        int firstFrame = Int32.Parse(startFrame[AnimationNumber]);
        //Log.Write("firstFrame: " + firstFrame);
        int lastFrame = Int32.Parse(endFrame[AnimationNumber]);
        //Log.Write("lastFrame: " + lastFrame);
        float NumberOfFrames = lastFrame - firstFrame;
        //Log.Write("NumberOfFrames: " + NumberOfFrames);
        float AnimationTimeToComplete = NumberOfFrames / 30.0f;
        //Log.Write("AnimationTimeToComplete: " + AnimationTimeToComplete);

        AnimationParameters animationParameters = initialAnimationParameters;

        if (PlaybackType[AnimationNumber].Contains("oop")) animationParameters.PlaybackMode = AnimationPlaybackMode.Loop;
        if (PlaybackType[AnimationNumber].Contains("ong")) animationParameters.PlaybackMode = AnimationPlaybackMode.PingPong;
        if (PlaybackType[AnimationNumber].Contains("nce")) animationParameters.PlaybackMode = AnimationPlaybackMode.PlayOnce;

        float fltPlaybackSpeed = float.Parse(AnimationSpeed[AnimationNumber]);
        animationParameters.PlaybackSpeed = Math.Abs(fltPlaybackSpeed) * Math.Sign(lastFrame - firstFrame);

        int intBlendDuration = Int32.Parse(BlendDuration[AnimationNumber]);
        if (intBlendDuration > 0) animationParameters.BlendDuration = Int32.Parse(BlendDuration[AnimationNumber]);

        //Log.Write("PlaybackSpeed: " + animationParameters.PlaybackSpeed);
        if (animationParameters.PlaybackSpeed > 0.0f)
        {
            animationParameters.RangeStartFrame = firstFrame;
            animationParameters.RangeEndFrame = lastFrame;
        }
        else
        {
            // Backwards playback uses negative playback speed but start frame still less than end frame
            animationParameters.RangeStartFrame = lastFrame;
            animationParameters.RangeEndFrame = firstFrame;
        }

        animationParameters.ClampToRange = true;
        float TimeAdjust = 1.0f / fltPlaybackSpeed;
        //Log.Write("Type Length: " + PlaybackType[AnimationNumber].Length);
        //Log.Write("Number: " + PlaybackType[AnimationNumber].Substring(4, PlaybackType[AnimationNumber].Length - 4));
        if (PlaybackType[AnimationNumber].Contains("oop"))
        {
            //you can say loop5 and it will loop 5 times
            if (PlaybackType[AnimationNumber].Length > 4)
            {
                int loopNum = Int32.Parse(PlaybackType[AnimationNumber].Substring(4, PlaybackType[AnimationNumber].Length - 4));
                int i = 0;
                do
                {
                    animation.Play(animationParameters);
                    //Log.Write("TimeAdjust: " + TimeAdjust);
                    Wait(TimeSpan.FromMilliseconds(AnimationTimeToComplete * 1000 * TimeAdjust));
                    i++;
                } while (i < loopNum);
            }
            else
            {
                //loop indefinitely
                animationParameters.PlaybackMode = AnimationPlaybackMode.Loop;
                animation.Play(animationParameters);
            }
        }
        else
        {
            //PlayOnce
            animation.Play(animationParameters);
            //Log.Write("TimeAdjust: " + TimeAdjust);
            Wait(TimeSpan.FromMilliseconds(AnimationTimeToComplete * 1000 * TimeAdjust));
        }

        if (killAnimation)
        {
            animation.Reset();
            killAnimation = false;
        }
        else
        {
            //string DoneEvent = AnimationDoneEvent[AnimationNumber];
            //SendToAll(DoneEvent, data.Data);
            //Log.Write("Sent Done Event: " + DoneEvent);
        }
    }

}


