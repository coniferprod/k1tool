using System;
using System.IO;
using System.Collections.Generic;

using KSynthLib.Common;
using KSynthLib.K1;

namespace k1tool
{
    public class SystemExclusiveHeader
    {
        public byte ManufacturerID;   // TODO: handle IDs with more than one byte
	    public byte Channel;
	    public byte Group;
	    public byte Function;
	    public byte MachineID;
	    public byte Substatus1;
	    public byte Substatus2;

        public SystemExclusiveHeader(byte[] data)
        {
            // First byte is the SysEx initiator F0h
            ManufacturerID = data[1];
            Channel = data[2];
            Function = data[3];
            Group = data[4];
            MachineID = data[5];
            Substatus1 = data[6];
            Substatus2 = data[7];
        }

        public override string ToString()
        {
            return String.Format("ManufacturerID = {0,2:X2}H, Channel = {1}, Function = {2,2:X2}H, Group = {3,2:X2}H, MachineID = {4,2:X2}H, Substatus1 = {5,2:X2}H, Substatus2 = {6,2:X2}H", ManufacturerID, Channel + 1, Function, Group, MachineID, Substatus1, Substatus2);
        }

        public byte[] Data
        {
            get
            {
                var buf = new List<byte>();
                buf.Add(Constants.SystemExclusiveInitiator);
                buf.Add(ManufacturerID);
                buf.Add(Channel);
                buf.Add(Function);
                buf.Add(Group);
                buf.Add(MachineID);
                buf.Add(Substatus1);
                buf.Add(Substatus2);
                // The data is specific to the command, so it is not inserted here.
                // Also, the SysEx terminator must be inserted by the caller, after the data.
                // So typically you would construct a SysEx message like this:
                // header.Data + <payload> + SystemExclusiveTerminator
                return buf.ToArray();
            }
        }
    }

    public enum SystemExclusiveFunction
    {
        OnePatchDataRequest,
        AllPatchDataRequest,
        OnePatchDataDump = 0x20,
        AllPatchDataDump = 0x21,
        MachineIDRequest = 0x60
    }

    // All single data dump: 8 + (32 * 88) + 1 = 8 + 2816 + 1 = 2825 bytes
    // All multi data dump:  8 + (32 * 76) + 1 = 8 + 2432 + 1 = 2441 bytes
    // All together (UPPERCASE singles + lowercase singles + multis) = 2825 + 2825 + 2441 = 8091
    class Program
    {
        public const int NumSingles = 32;
        public const int NumMultis = 32;

        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                System.Console.WriteLine("Usage: K5KTool cmd filename.syx");
                return 1;
            }

            string command = args[0];
            string fileName = args[1];
            string patchName = "";
            if (args.Length > 2)
            {
                patchName = args[2];
            }

            byte[] fileData = File.ReadAllBytes(fileName);
            System.Console.WriteLine($"SysEx file: '{fileName}' ({fileData.Length} bytes)");

            List<byte[]> messages = Util.SplitBytesByDelimiter(fileData, Constants.SystemExclusiveTerminator);
            System.Console.WriteLine($"Got {messages.Count} messages");

            foreach (byte[] message in messages)
            {
                SystemExclusiveHeader header = new SystemExclusiveHeader(message);
                //System.Console.WriteLine(Util.HexDump(header.Data));
                System.Console.WriteLine(header.ToString());
                int headerLength = header.Data.Length;

                // Examine the header to see what kind of message this is:
                SystemExclusiveFunction function = (SystemExclusiveFunction)header.Function;
                if (function == SystemExclusiveFunction.AllPatchDataDump)
                {
                    // sub2: 0=I or E, 20H=i or e singles
                    if (header.Substatus2 == 0x00 || header.Substatus2 == 0x20)
                    {
                        int offset = headerLength;
                        for (int i = 0; i < NumSingles; i++)
                        {
                            byte[] singleData = new byte[SinglePatch.DataSize];
                            Buffer.BlockCopy(message, offset, singleData, 0, SinglePatch.DataSize);
                            System.Console.WriteLine("INGOING SINGLE DATA = \n" + Util.HexDump(singleData));
                            SinglePatch single = new SinglePatch(singleData);
                            System.Console.WriteLine(single.ToString());

                            byte[] sysExData = single.ToData();
                            System.Console.WriteLine("OUTCOMING SINGLE DATA = \n" + Util.HexDump(sysExData));

                            (bool result, int diffIndex) = Util.ByteArrayCompare(singleData, sysExData);
                            System.Console.WriteLine(String.Format("match = {0}, diff index = {1}", result ? "YES :-)" : "NO :-(", diffIndex));

                            offset += SinglePatch.DataSize;
                        }
                    }
                    else if (header.Substatus2 == 0x40)
                    {
                        System.Console.WriteLine("Multis not handled yet");
                        return 1;
                    }
                }
                else if (function == SystemExclusiveFunction.OnePatchDataDump)
                {
                    System.Console.WriteLine("One patch dumps not handled yet");
                    return 1;
                }
            }

            return 0;
        }
    }
}
