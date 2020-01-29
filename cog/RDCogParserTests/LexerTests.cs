using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RDCogParserTests
{
    [TestClass]
    public class LexerTests
    {
        [TestMethod]
        public void OnlyCommentsYieldZeroTokens()
        {
            var input = @"# Jedi Knight Cog Script
#
# 00_DOORKEY.COG
#
# Multiple Doors opened with a key
# Use 0 in ""key"" for the Red Key (default), 1 for Blue and 2 for Yellow
#
# [YB]
#
# 8/28/97 Added clicking sounds [DB]
#
# (C) 1997 LucasArts Entertainment Co. All Rights Reserved";

            var tokens = RDCogParser.Lexer.Tokenize(input);
            Assert.IsTrue(tokens.Count == 0);
        }

        [TestMethod]
        public void SomeTokens()
        {
            var input = @"thing       door0                            linkid=0 mask=0x405";

            var tokens = RDCogParser.Lexer.Tokenize(input);
            Assert.IsTrue(tokens.Count == 8);
        }

        [TestMethod]
        public void MultipleLinesWithSymbols()
        {
            var input = @"thing       door0                            linkid=0 mask=0x405 
thing       door1                            linkid=1 mask=0x405";

            var tokens = RDCogParser.Lexer.Tokenize(input);
            Assert.IsTrue(tokens.Count == 17);
        }

        [TestMethod]
        public void SymbolWithFilename()
        {
            var input = @"sound       locked_snd=i00ky73t.wav          local";

            var tokens = RDCogParser.Lexer.Tokenize(input);
            Assert.IsTrue(tokens.Count == 5);
        }

        [TestMethod]
        public void Message()
        {
            var input = @"startup:
   if (door0 >= 0) numdoors = numdoors + 1;
   doorsector = GetThingSector(door0);

   Return;";

            var tokens = RDCogParser.Lexer.Tokenize(input);
            Assert.IsTrue(tokens.Count == 28);
        }

        [TestMethod]
        public void EntireFile()
        {
            var input = @"# Jedi Knight Cog Script
#
# 00_DOORKEY.COG
#
# Multiple Doors opened with a key
# Use 0 in ""key"" for the Red Key (default), 1 for Blue and 2 for Yellow
#
# [YB]
#
# 8/28/97 Added clicking sounds [DB]
#
# (C) 1997 LucasArts Entertainment Co. All Rights Reserved


symbols

thing       door0 linkid = 0 mask = 0x405
thing door1                            linkid = 1 mask = 0x405
thing door2                            linkid = 2 mask = 0x405
thing door3                            linkid = 3 mask = 0x405

flex movespeed = 8.0
flex sleeptime = 2.0
flex lightvalue = 0.5
int key = 0

sound locked_snd = i00ky73t.wav          local
sound       wav0 = lvrclik2.wav

sector doorsector                       local
int numdoors                         local
int doorstatus                       local
int movestatus                       local
int player                           local

flex        lasttime = -1                      local
flex        curtime = -1                       local

message     startup
message     activated
message     arrived
message     blocked
message     timer

end

# ========================================================================================

code

startup:
   if (door0 >= 0) numdoors = numdoors + 1;
            if (door1 >= 0) numdoors = numdoors + 1;
            if (door2 >= 0) numdoors = numdoors + 1;
            if (door3 >= 0) numdoors = numdoors + 1;

            doorsector = GetThingSector(door0);
            SectorAdjoins(doorsector, 0);
            SectorLight(doorsector, lightvalue, 0.0); // add some light to door sector

            Return;

# ........................................................................................

        activated:
            player = jkGetLocalPlayer();

            if ((GetInv(player, 46 + key) == 1.0) || (GetSourceRef() != player))       // if player has the needed key
            {                                         // or enemy triggers door
                call checkstatus;
                if (movestatus) return;
                if (doorstatus == 0)
                {                                      // all pieces are at frame 0
                    SectorAdjoins(doorsector, 1);
                    // show the key icon for 2 seconds
                    SetInvActivated(player, 46 + key, 1);
                    SetTimerEx(2, 1, 0, 0);
                    // PlaySoundThing(key_snd, player, 1.0, -1, -1, 0);
                    call open_doors;
                }
            }
            else
            {
                PlaySoundThing(wav0, door0, 1.0, -1, -1, 0);
                curtime = GetLevelTime();
                if ((lasttime == -1) || (curtime - lasttime > 3))
                {
                    PlaySoundThing(locked_snd, player, 1.0, -1, -1, 0);
                    jkPrintUNIString(-1, 1001);
                    lasttime = curtime;
                }
            }

            Return;

# ........................................................................................

        arrived:
            call checkstatus;
            if (movestatus) return;
            if (doorstatus == numdoors)
            {                                         // all pieces are at frame 1
                sleep(sleeptime);
                call close_doors;
            }
            else if (doorstatus == 0)
            {                                         // all pieces are at frame 0
                sectoradjoins(doorsector, 0);
            }

            Return;

# ........................................................................................

        blocked:
            call open_doors;

            Return;

# ........................................................................................

        timer:
            // Remove the key icon
            SetInvActivated(player, 46 + key, 0);
            Return;

# ........................................................................................

        open_doors:
            MoveToFrame(door0, 1, movespeed);
            if (door1 >= 0) MoveToFrame(door1, 1, movespeed);
            if (door2 >= 0) MoveToFrame(door2, 1, movespeed);
            if (door3 >= 0) MoveToFrame(door3, 1, movespeed);

            Return;


        close_doors:
            MoveToFrame(door0, 0, movespeed);
            if (door1 >= 0) MoveToFrame(door1, 0, movespeed);
            if (door2 >= 0) MoveToFrame(door2, 0, movespeed);
            if (door3 >= 0) MoveToFrame(door3, 0, movespeed);

            Return;


        checkstatus:
            movestatus = IsThingMoving(door0);
            if (door1 >= 0) movestatus = movestatus + IsThingMoving(door1);
            if (door2 >= 0) movestatus = movestatus + IsThingMoving(door2);
            if (door3 >= 0) movestatus = movestatus + IsThingMoving(door3);

            doorstatus = GetCurFrame(door0);
            if (door1 >= 0) doorstatus = doorstatus + GetCurFrame(door1);
            if (door2 >= 0) doorstatus = doorstatus + GetCurFrame(door2);
            if (door3 >= 0) doorstatus = doorstatus + GetCurFrame(door3);

            Return;

            end

";

            var tokens = RDCogParser.Lexer.Tokenize(input);
            Assert.IsTrue(tokens.Count == 28);
        }
    }
}
