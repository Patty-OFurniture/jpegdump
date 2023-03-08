// Copyright (c) Victor Derks.
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.IO;

namespace JpegDump
{
    enum JpegMarker
    {
        TEM = 0x01,
        // Start of Frame - descriptions from CCITT ITU T.81 09/92
        StartOfFrame0 = 0xc0,  // baseline DCT process frame marker
        StartOfFrame1,  // extended sequential DCT frame marker, Huffman coding
        StartOfFrame2,  // progressive DCT frame marker, Huffman coding
        StartOfFrame3,  // lossless process frame marker, Huffman coding
        DHT = 0xc4,
        StartOfFrame5,  // differential sequential DCT frame marker, Huffman coding
        StartOfFrame6,  // differential progressive DCT frame marker, Huffman coding
        StartOfFrame7,  // differential lossless process frame marker, Huffman coding
        JPGA = 0xc8,
        StartOfFrame9,  // sequential DCT frame marker, arithmetic coding
        StartOfFrame10, // progressive DCT frame marker, arithmetic coding
        StartOfFrame11, // lossless process frame marker, arithmetic coding
        DAC = 0xcc,
        StartOfFrame13, // differential sequential DCT frame marker, arithmetic coding
        StartOfFrame14, // differential progressive DCT frame marker, arithmetic coding
        StartOfFrame15, // differential lossless process frame marker, arithmetic coding
        Restart0 = 0xD0,                   // RST0
        Restart1 = 0xD1,                   // RST1
        Restart2 = 0xD2,                   // RST2
        Restart3 = 0xD3,                   // RST3
        Restart4 = 0xD4,                   // RST4
        Restart5 = 0xD5,                   // RST5
        Restart6 = 0xD6,                   // RST6
        Restart7 = 0xD7,                   // RST7
        StartOfImage = 0xD8,               // SOI
        EndOfImage = 0xD9,                 // EOI
        StartOfScan = 0xDA,                // SOS
        DefineQuantizationTable,           // DQT
        DefineNumberofLines,               // DNL
        DefineRestartInterval = 0xDD,      // DRI
        DHP = 0xde,
        EXP = 0xdf,
        APP = 0xe0,
        COM = 0xfe,
        /* marker codes added by JPEG Part 3 extensions */
        VER = 0xf0,
        DTI = 0xf1,
        DTT = 0xf2,
        SRF = 0xf3,
        SRS = 0xf4,
        DCR = 0xf5,
        DQS = 0xf6,
        // back to original code
        StartOfFrameJpegLS = 0xF7,         // SOF_55: Marks the start of a (JPEG-LS) encoded frame.
        JpegLSExtendedParameters = 0xF8,   // LSE: JPEG-LS extended parameters.
        ApplicationData0 = 0xE0,           // APP0: Application data 0: used for JFIF header.
        ApplicationData7 = 0xE7,           // APP7: Application data 7: color space.
        ApplicationData8 = 0xE8,           // APP8: Application data 8: colorXForm.
        ApplicationData14 = 0xEE,          // APP14: Application data 14: used by Adobe
        Comment = 0xFE                     // COM:  Comment block.
    }

    internal class JpegStreamReader : IDisposable
    {
        private readonly BinaryReader reader;
        private bool jpegLSStream;

        public JpegStreamReader(Stream stream)
        {
            reader = new BinaryReader(stream);
        }

        public void Dispose()
        {
            reader.Dispose();
        }

        public void Dump()
        {
            int c;
            while ((c = reader.BaseStream.ReadByte()) != -1)
            {
                if (c == 0xFF)
                {
                    int markerCode = reader.BaseStream.ReadByte();
                    if (IsMarkerCode(markerCode))
                    {
                        DumpMarker(markerCode);
                    }
                }
            }
        }

        private bool IsMarkerCode(int code)
        {
            // To prevent marker codes in the encoded bit stream encoders must encode the next byte zero or the next bit zero (jpeg-ls).
            if (jpegLSStream)
                return (code & 0x80) == 0X80;

            return code > 0;
        }

        private void DumpMarker(int markerCode)
        {
            //  FFD0 to FFD9 and FF01, markers without size.

            switch ((JpegMarker)markerCode)
            {
                case JpegMarker.Restart0:
                case JpegMarker.Restart1:
                case JpegMarker.Restart2:
                case JpegMarker.Restart3:
                case JpegMarker.Restart4:
                case JpegMarker.Restart5:
                case JpegMarker.Restart6:
                case JpegMarker.Restart7:
                    Console.WriteLine("{0:D8} Marker 0xFF{1:X}. RST{2} (Restart Marker {2}), defined in ITU T.81/IEC 10918-1",
                        GetStartOffset(), markerCode, markerCode - JpegMarker.Restart0);
                    break;

                case JpegMarker.StartOfImage:
                    DumpStartOfImageMarker();
                    break;

                case JpegMarker.EndOfImage:
                    Console.WriteLine("{0:D8} Marker 0xFFD9. EOI (End Of Image), defined in ITU T.81/IEC 10918-1", GetStartOffset());
                    break;

                case JpegMarker.StartOfFrameJpegLS:
                    jpegLSStream = true;
                    DumpStartOfFrameJpegLS();
                    break;

                case JpegMarker.JpegLSExtendedParameters:
                    DumpJpegLSExtendedParameters();
                    break;

                case JpegMarker.StartOfScan:
                    DumpStartOfScan();
                    break;

                case JpegMarker.DefineRestartInterval:
                    DumpDefineRestartInterval();
                    break;

                case JpegMarker.ApplicationData0:
                    Console.WriteLine("{0:D8} Marker 0xFFE0. App0 (Application Data 0), defined in ITU T.81/IEC 10918-1", GetStartOffset());
                    break;

                case JpegMarker.ApplicationData0 + 1:
                    DumpApplicationData1();
                    break;

                case JpegMarker.ApplicationData0 + 2:
                    DumpApplicationData2();
                    break;

                case JpegMarker.ApplicationData0 + 3:
                    DumpApplicationData3();
                    break;

                case JpegMarker.ApplicationData0 + 4:
                    DumpApplicationData4();
                    break;

                case JpegMarker.ApplicationData0 + 5:
                    DumpApplicationData5();
                    break;

                case JpegMarker.ApplicationData0 + 6:
                    DumpApplicationData6();
                    break;

                case JpegMarker.ApplicationData0 + 7:
                    DumpApplicationData7();
                    break;

                case JpegMarker.ApplicationData0 + 8:
                    DumpApplicationData8();
                    break;

                case JpegMarker.ApplicationData0 + 9:
                    DumpApplicationData9();
                    break;

                case JpegMarker.ApplicationData0 + 10:
                    DumpApplicationData10();
                    break;

                case JpegMarker.ApplicationData0 + 11:
                    DumpApplicationData11();
                    break;

                case JpegMarker.ApplicationData0 + 12:
                    DumpApplicationData12();
                    break;

                case JpegMarker.ApplicationData0 + 13:
                    DumpApplicationData13();
                    break;

                case JpegMarker.ApplicationData0 + 14:
                    DumpApplicationData14();
                    break;

                case JpegMarker.ApplicationData0 + 15:
                    DumpApplicationData15();
                    break;


                case JpegMarker.StartOfFrame0:
                    Console.WriteLine("{0:D8} Marker 0xFF{1:X}: {2} baseline DCT process frame marker", GetStartOffset(), markerCode, typeof(JpegMarker).GetEnumName(markerCode));
                    break;

                case JpegMarker.StartOfFrame1:
                    Console.WriteLine("{0:D8} Marker 0xFF{1:X}: {2} extended sequential DCT frame marker, Huffman coding", GetStartOffset(), markerCode, typeof(JpegMarker).GetEnumName(markerCode));
                    break;

                case JpegMarker.StartOfFrame2:
                    Console.WriteLine("{0:D8} Marker 0xFF{1:X}: {2} progressive DCT frame marker, Huffman coding", GetStartOffset(), markerCode, typeof(JpegMarker).GetEnumName(markerCode));
                    break;

                case JpegMarker.StartOfFrame3:
                    Console.WriteLine("{0:D8} Marker 0xFF{1:X}: {2} lossless process frame marker, Huffman coding", GetStartOffset(), markerCode, typeof(JpegMarker).GetEnumName(markerCode));
                    break;

                case JpegMarker.StartOfFrame5:
                    Console.WriteLine("{0:D8} Marker 0xFF{1:X}: {2} differential sequential DCT frame marker, Huffman coding", GetStartOffset(), markerCode, typeof(JpegMarker).GetEnumName(markerCode));
                    break;

                case JpegMarker.StartOfFrame6:
                    Console.WriteLine("{0:D8} Marker 0xFF{1:X}: {2} differential progressive DCT frame marker, Huffman coding", GetStartOffset(), markerCode, typeof(JpegMarker).GetEnumName(markerCode));
                    break;

                case JpegMarker.StartOfFrame7:
                    Console.WriteLine("{0:D8} Marker 0xFF{1:X}: {2} differential lossless process frame marker, Huffman coding", GetStartOffset(), markerCode, typeof(JpegMarker).GetEnumName(markerCode));
                    break;

                case JpegMarker.StartOfFrame9:
                    Console.WriteLine("{0:D8} Marker 0xFF{1:X}: {2} sequential DCT frame marker, arithmetic coding", GetStartOffset(), markerCode, typeof(JpegMarker).GetEnumName(markerCode));
                    break;

                case JpegMarker.StartOfFrame10:
                    Console.WriteLine("{0:D8} Marker 0xFF{1:X}: {2} progressive DCT frame marker, arithmetic coding", GetStartOffset(), markerCode, typeof(JpegMarker).GetEnumName(markerCode));
                    break;

                case JpegMarker.StartOfFrame11:
                    Console.WriteLine("{0:D8} Marker 0xFF{1:X}: {2} lossless process frame marker, arithmetic coding", GetStartOffset(), markerCode, typeof(JpegMarker).GetEnumName(markerCode));
                    break;

                case JpegMarker.StartOfFrame13:
                    Console.WriteLine("{0:D8} Marker 0xFF{1:X}: {2} differential sequential DCT frame marker, arithmetic coding", GetStartOffset(), markerCode, typeof(JpegMarker).GetEnumName(markerCode));
                    break;

                case JpegMarker.StartOfFrame14:
                    Console.WriteLine("{0:D8} Marker 0xFF{1:X}: {2} differential progressive DCT frame marker, arithmetic coding", GetStartOffset(), markerCode, typeof(JpegMarker).GetEnumName(markerCode));
                    break;

                case JpegMarker.StartOfFrame15:
                    Console.WriteLine("{0:D8} Marker 0xFF{1:X}: {2} differential lossless process frame marker, arithmetic coding", GetStartOffset(), markerCode, typeof(JpegMarker).GetEnumName(markerCode));
                    break;

                case JpegMarker.Comment:
                    Console.WriteLine("{0:D8} Marker 0xFFFE. COM (Comment), defined in ITU T.81/IEC 10918-1", GetStartOffset());
                    break;

                default:
                    Console.WriteLine("{0:D8} Marker 0xFF{2:X}: {1}", GetStartOffset(), typeof(JpegMarker).GetEnumName(markerCode), markerCode);
                    break;
            }
        }

        private long GetStartOffset()
        {
            return reader.BaseStream.Position - 2;
        }

        private long Position
        {
            get
            {
                return reader.BaseStream.Position;
            }
        }


        private void DumpStartOfImageMarker()
        {
            Console.WriteLine("{0:D8} Marker 0xFFD8: SOI (Start Of Image), defined in ITU T.81/IEC 10918-1", GetStartOffset());
        }

        private void DumpStartOfFrameJpegLS()
        {
            Console.WriteLine("{0:D8} Marker 0xFFF7: SOF_55 (Start Of Frame JPEG-LS), defined in ITU T.87/IEC 14495-1 JPEG LS", GetStartOffset());
            Console.WriteLine("{0:D8}  Size = {1}", Position, ReadUInt16BigEndian());
            Console.WriteLine("{0:D8}  Sample precision (P) = {1}", Position, reader.ReadByte());
            Console.WriteLine("{0:D8}  Number of lines (Y) = {1}", Position, ReadUInt16BigEndian());
            Console.WriteLine("{0:D8}  Number of samples per line (X) = {1}", Position, ReadUInt16BigEndian());
            long position = Position;
            byte componentCount = reader.ReadByte();
            Console.WriteLine("{0:D8}  Number of image components in a frame (Nf) = {1}", position, componentCount);
            for (int i = 0; i < componentCount; i++)
            {
                Console.WriteLine("{0:D8}   Component identifier (Ci) = {1}", Position, reader.ReadByte());

                position = Position;
                byte samplingFactor = reader.ReadByte();
                Console.WriteLine("{0:D8}   H and V sampling factor (Hi + Vi) = {1} ({2} + {3})", position, samplingFactor, samplingFactor >> 4, samplingFactor & 0xF);
                Console.WriteLine("{0:D8}   Quantization table (Tqi) [reserved, should be 0] = {1}", Position, reader.ReadByte());
            }
        }

        private void DumpJpegLSExtendedParameters()
        {
            Console.WriteLine("{0:D8} Marker 0xFFF8: LSE (JPEG-LS ), defined in ITU T.87/IEC 14495-1 JPEG LS", GetStartOffset());
            Console.WriteLine("{0:D8}  Size = {1}", Position, ReadUInt16BigEndian());
            byte type = reader.ReadByte();

            Console.Write("{0:D8}  Type = {1}", Position, type);
            switch (type)
            {
                case 1:
                    Console.WriteLine(" (Preset coding parameters)");
                    Console.WriteLine("{0:D8}  MaximumSampleValue = {1}", Position, ReadUInt16BigEndian());
                    Console.WriteLine("{0:D8}  Threshold 1 = {1}", Position, ReadUInt16BigEndian());
                    Console.WriteLine("{0:D8}  Threshold 2 = {1}", Position, ReadUInt16BigEndian());
                    Console.WriteLine("{0:D8}  Threshold 3 = {1}", Position, ReadUInt16BigEndian());
                    Console.WriteLine("{0:D8}  Reset value = {1}", Position, ReadUInt16BigEndian());
                    break;

                default:
                    Console.WriteLine(" (Unknown");
                    break;
            }
        }

        private void DumpStartOfScan()
        {
            Console.WriteLine("{0:D8} Marker 0xFFDA: SOS (Start Of Scan), defined in ITU T.81/IEC 10918-1", GetStartOffset());
            Console.WriteLine("{0:D8}  Size = {1}", Position, ReadUInt16BigEndian());
            byte componentCount = reader.ReadByte();
            Console.WriteLine("{0:D8}  Component Count = {1}", Position, componentCount);
            for (int i = 0; i < componentCount; i++)
            {
                Console.WriteLine("{0:D8}   Component identifier (Ci) = {1}", Position, reader.ReadByte());
                byte mappingTableSelector = reader.ReadByte();
                Console.WriteLine("{0:D8}   Mapping table selector = {1} {2}", Position, mappingTableSelector, mappingTableSelector == 0 ? "(None)" : string.Empty);
            }

            Console.WriteLine("{0:D8}  Near lossless (NEAR parameter) = {1}", Position, reader.ReadByte());
            byte interleaveMode = reader.ReadByte();
            Console.WriteLine("{0:D8}  Interleave mode (ILV parameter) = {1} ({2})", Position, interleaveMode, GetInterleaveModeName(interleaveMode));
            Console.WriteLine("{0:D8}  Point Transform = {1}", Position, reader.ReadByte());
        }

        private void DumpDefineRestartInterval()
        {
            Console.WriteLine("{0:D8} Marker 0xFFDD: DRI (Define Restart Interval), defined in ITU T.81/IEC 10918-1", GetStartOffset());
            ushort size = ReadUInt16BigEndian();
            Console.WriteLine("{0:D8}  Size = {1}", Position, size);

            // ISO/IEC 14495-1, C.2.5 extends DRI to allow usage of 2-4 bytes for the interval.
            switch (size)
            {
                case 4:
                    Console.WriteLine("{0:D8}  Restart Interval = {1}", Position, ReadUInt16BigEndian());
                    break;

                case 5:
                    Console.WriteLine("{0:D8}  Restart Interval = {1}", Position, ReadUInt24BigEndian());
                    break;

                case 6:
                    Console.WriteLine("{0:D8}  Restart Interval = {1}", Position, ReadUInt32BigEndian());
                    break;

                default:
                    break;
            }
        }

        private void DumpApplicationData1()
        {
            int appId = 1;
            Console.WriteLine("{0:D8} Marker 0xFFE{1:x}: APP{1} (Application Data {1}), defined in ITU T.81/IEC 10918-1", GetStartOffset(), appId);
            int size = ReadUInt16BigEndian();
            Console.WriteLine("{0:D8}  Size = {1}", Position, size);
            byte[] dataBytes = reader.ReadBytes(size - 2);
        }

        private void DumpApplicationData2()
        {
            int appId = 2;
            Console.WriteLine("{0:D8} Marker 0xFFE{1:x}: APP{1} (Application Data {1}), defined in ITU T.81/IEC 10918-1", GetStartOffset(), appId);
            int size = ReadUInt16BigEndian();
            Console.WriteLine("{0:D8}  Size = {1}", Position, size);
            byte[] dataBytes = reader.ReadBytes(size - 2);
        }

        private void DumpApplicationData3()
        {
            int appId = 3;
            Console.WriteLine("{0:D8} Marker 0xFFE{1:x}: APP{1} (Application Data {1}), defined in ITU T.81/IEC 10918-1", GetStartOffset(), appId);
            int size = ReadUInt16BigEndian();
            Console.WriteLine("{0:D8}  Size = {1}", Position, size);
            byte[] dataBytes = reader.ReadBytes(size - 2);
        }

        private void DumpApplicationData4()
        {
            int appId = 4;
            Console.WriteLine("{0:D8} Marker 0xFFE{1:x}: APP{1} (Application Data {1}), defined in ITU T.81/IEC 10918-1", GetStartOffset(), appId);
            int size = ReadUInt16BigEndian();
            Console.WriteLine("{0:D8}  Size = {1}", Position, size);
            byte[] dataBytes = reader.ReadBytes(size - 2);
        }

        private void DumpApplicationData5()
        {
            int appId = 5;
            Console.WriteLine("{0:D8} Marker 0xFFE{1:x}: APP{1} (Application Data {1}), defined in ITU T.81/IEC 10918-1", GetStartOffset(), appId);
            int size = ReadUInt16BigEndian();
            Console.WriteLine("{0:D8}  Size = {1}", Position, size);
            byte[] dataBytes = reader.ReadBytes(size - 2);
        }

        private void DumpApplicationData6()
        {
            int appId = 6;
            Console.WriteLine("{0:D8} Marker 0xFFE{1:x}: APP{1} (Application Data {1}), defined in ITU T.81/IEC 10918-1", GetStartOffset(), appId);
            int size = ReadUInt16BigEndian();
            Console.WriteLine("{0:D8}  Size = {1}", Position, size);
            byte[] dataBytes = reader.ReadBytes(size - 2);
        }

        private void DumpApplicationData7()
        {
            int appId = 7;
            Console.WriteLine("{0:D8} Marker 0xFFE{1:x}: APP{1} (Application Data {1}), defined in ITU T.81/IEC 10918-1", GetStartOffset(), appId);
            int size = ReadUInt16BigEndian();
            Console.WriteLine("{0:D8}  Size = {1}", Position, size);
            byte[] dataBytes = reader.ReadBytes(size - 2);

            TryDumpAsHPColorSpace(dataBytes);
        }

        private void DumpApplicationData8()
        {
            int appId = 8;
            Console.WriteLine("{0:D8} Marker 0xFFE{1:x}: APP{1} (Application Data {1}), defined in ITU T.81/IEC 10918-1", GetStartOffset(), appId);
            int size = ReadUInt16BigEndian();
            Console.WriteLine("{0:D8}  Size = {1}", Position, size);
            byte[] dataBytes = reader.ReadBytes(size - 2);

            if (TryDumpAsSpiffHeader(dataBytes))
                return;

            if (TryDumpAsSpiffEndOfDirectory(dataBytes))
                return;

            TryDumpAsHPColorTransformation(dataBytes);
        }

        private void DumpApplicationData9()
        {
            int appId = 9;
            Console.WriteLine("{0:D8} Marker 0xFFE{1:x}: APP{1} (Application Data {1}), defined in ITU T.81/IEC 10918-1", GetStartOffset(), appId);
            int size = ReadUInt16BigEndian();
            Console.WriteLine("{0:D8}  Size = {1}", Position, size);
            byte[] dataBytes = reader.ReadBytes(size - 2);
        }

        private void DumpApplicationData10()
        {
            int appId = 10;
            Console.WriteLine("{0:D8} Marker 0xFFE{1:x}: APP{1} (Application Data {1}), defined in ITU T.81/IEC 10918-1", GetStartOffset(), appId);
            int size = ReadUInt16BigEndian();
            Console.WriteLine("{0:D8}  Size = {1}", Position, size);
            byte[] dataBytes = reader.ReadBytes(size - 2);
        }

        private void DumpApplicationData11()
        {
            int appId = 11;
            Console.WriteLine("{0:D8} Marker 0xFFE{1:x}: APP{1} (Application Data {1}), defined in ITU T.81/IEC 10918-1", GetStartOffset(), appId);
            int size = ReadUInt16BigEndian();
            Console.WriteLine("{0:D8}  Size = {1}", Position, size);
            byte[] dataBytes = reader.ReadBytes(size - 2);
        }

        private void DumpApplicationData12()
        {
            int appId = 12;
            Console.WriteLine("{0:D8} Marker 0xFFE{1:x}: APP{1} (Application Data {1}), defined in ITU T.81/IEC 10918-1", GetStartOffset(), appId);
            int size = ReadUInt16BigEndian();
            Console.WriteLine("{0:D8}  Size = {1}", Position, size);
            byte[] dataBytes = reader.ReadBytes(size - 2);
        }

        private void DumpApplicationData13()
        {
            int appId = 13;
            Console.WriteLine("{0:D8} Marker 0xFFE{1:x}: APP{1} (Application Data {1}), defined in ITU T.81/IEC 10918-1", GetStartOffset(), appId);
            int size = ReadUInt16BigEndian();
            Console.WriteLine("{0:D8}  Size = {1}", Position, size);
            byte[] dataBytes = reader.ReadBytes(size - 2);

            TryDumpAsAdobeApp13(dataBytes, Position - dataBytes.Length);
        }

        private void DumpApplicationData14()
        {
            int appId = 14;
            Console.WriteLine("{0:D8} Marker 0xFFE{1:x}: APP{1} (Application Data {1}), defined in ITU T.81/IEC 10918-1", GetStartOffset(), appId);
            int size = ReadUInt16BigEndian();
            Console.WriteLine("{0:D8}  Size = {1}", Position - 2, size);
            byte[] dataBytes = reader.ReadBytes(size - 2);

            TryDumpAsAdobeApp14(dataBytes, Position - dataBytes.Length);
        }

        private void DumpApplicationData15()
        {
            int appId = 15;
            Console.WriteLine("{0:D8} Marker 0xFFE{1:x}: APP{1} (Application Data {1}), defined in ITU T.81/IEC 10918-1", GetStartOffset(), appId);
            int size = ReadUInt16BigEndian();
            Console.WriteLine("{0:D8}  Size = {1}", Position, size);
            byte[] dataBytes = reader.ReadBytes(size - 2);
        }

        private bool TryDumpAsSpiffHeader(IReadOnlyList<byte> dataBuffer)
        {
            if (dataBuffer.Count < 30)
                return false;

            if (!(dataBuffer[0] == 'S' && dataBuffer[1] == 'P' && dataBuffer[2] == 'I' && dataBuffer[3] == 'F' && dataBuffer[4] == 'F'))
                return false;

            Console.WriteLine("{0:D8}  SPIFF Header, defined in ISO/IEC 10918-3, Annex F", GetStartOffset() - 28);
            Console.WriteLine("{0:D8}  High version = {1}", GetStartOffset() - 26, dataBuffer[6]);
            Console.WriteLine("{0:D8}  Low version = {1}", GetStartOffset() - 25, dataBuffer[7]);
            Console.WriteLine("{0:D8}  Profile id = {1}", GetStartOffset() - 24, dataBuffer[8]);
            Console.WriteLine("{0:D8}  Component count = {1}", GetStartOffset() - 23, dataBuffer[9]);
            Console.WriteLine("{0:D8}  Height = {1}", GetStartOffset() - 22, ConvertToUint32BigEndian(dataBuffer, 10));
            Console.WriteLine("{0:D8}  Width = {1}", GetStartOffset() - 18, ConvertToUint32BigEndian(dataBuffer, 14));
            Console.WriteLine("{0:D8}  Color Space = {1} ({2})", GetStartOffset() - 14, dataBuffer[18], GetColorSpaceName(dataBuffer[18]));
            Console.WriteLine("{0:D8}  Bits per sample = {1}", GetStartOffset() - 13, dataBuffer[19]);
            Console.WriteLine("{0:D8}  Compression Type = {1} ({2})", GetStartOffset() - 12, dataBuffer[20], GetCompressionTypeName(dataBuffer[20]));
            Console.WriteLine("{0:D8}  Resolution Units = {1} ({2})", GetStartOffset() - 11, dataBuffer[21], GetResolutionUnitsName(dataBuffer[21]));
            Console.WriteLine("{0:D8}  Vertical resolution = {1}", GetStartOffset() - 10, ConvertToUint32BigEndian(dataBuffer, 22));
            Console.WriteLine("{0:D8}  Horizontal resolution = {1}", GetStartOffset() - 6, ConvertToUint32BigEndian(dataBuffer, 26));

            return true;
        }

        private bool TryDumpAsSpiffEndOfDirectory(IReadOnlyList<byte> dataBuffer)
        {
            if (dataBuffer.Count != 6)
                return false;

            uint entryType = ConvertToUint32BigEndian(dataBuffer, 0);
            if (entryType == 1)
            {
                Console.WriteLine("{0:D8}  SPIFF EndOfDirectory Entry, defined in ISO/IEC 10918-3, Annex F",
                    GetStartOffset() - 4);
            }

            return true;
        }

        private void TryDumpAsHPColorTransformation(IReadOnlyList<byte> dataBuffer)
        {
            if (dataBuffer.Count != 5)
                return;

            // Check for 'xfrm' stored in little endian
            if (!(dataBuffer[0] == 0x6D && dataBuffer[1] == 0x72 && dataBuffer[2] == 0x66 && dataBuffer[3] == 0x78))
                return;

            Console.WriteLine("{0:D8}  HP colorXForm, defined by HP JPEG-LS implementation", GetStartOffset() - 3);
            Console.WriteLine("{0:D8}  Transformation = {1} ({2})", GetStartOffset(), dataBuffer[4], GetHPColorTransformationName(dataBuffer[4]));
        }

        private static void TryDumpAsAdobeApp13(IReadOnlyList<byte> dataBuffer, long startPosition)
        {
            //if (dataBuffer.Count != 5 + 2 + 2 + 2 + 1)
            //    return;

            // Check for 'Photoshop marker'
            string magic = "Photoshop 3.0";
            string signature = System.Text.Encoding.ASCII.GetString((byte[])dataBuffer, 0, magic.Length);

            if (signature == magic)
            {
                Console.WriteLine("{0:D8}  APP13 'Photoshop' identifier", startPosition);
                int index = magic.Length + 1;
                /* 10 bytes fixed data, plus a counted string padded to even length */
                while (index < dataBuffer.Count)
                {
                    string OSType = System.Text.Encoding.ASCII.GetString((byte[])dataBuffer, index, 4);
                    /* print the OSType as text -- it should always be '8BIM' */
                    Console.WriteLine("{0:D8}  OSType: {1}", startPosition, OSType);

                    uint resourceType = ConvertToUint16FromBigEndian(dataBuffer, index + 4);
                    string resourceName = "";
                    int nameLength = dataBuffer[index + 6];
                    if (nameLength > 0)
                    {
                        resourceName = System.Text.Encoding.ASCII.GetString((byte[])dataBuffer, index + 7, nameLength);
                    }
                    index += 7 + nameLength;

                    if ((index & 1) == 1)
                        index++;

                    uint resourceSize = ConvertToUint32BigEndian(dataBuffer, index);
                    index += 4;
                    if ((resourceSize & 1) == 1)
                        resourceSize++;

                    Console.WriteLine("{0:D8}  Resource {1:x} {2}", startPosition, resourceType, resourceName);
                    Console.WriteLine("{0:D8}  Size = {1}", startPosition, resourceSize);
                    while (index < dataBuffer.Count)
                    {

                        uint segmentMarker = ConvertToUint16FromBigEndian(dataBuffer, index);
                        uint segmentType = dataBuffer[index + 2];
                        uint segmentSize = ConvertToUint16FromBigEndian(dataBuffer, index + 3);

                        index = index + 5;
                        string segmentText = "";
                        if (index + segmentSize <= dataBuffer.Count)
                            segmentText = System.Text.Encoding.ASCII.GetString((byte[])dataBuffer, index, (int)segmentSize);

                        if (segmentType == 0x28)
                        {
                            // Special Instructions
                            if (segmentText.StartsWith("FBMD"))
                            {
                                TryDumpFbmd(segmentText);
                            }
                        }
                        else if (segmentType == 0x67)
                        {
                            // Original Transmission Reference
                            Console.WriteLine("{0:D8}  Original Transmission Reference  Text: {1}", startPosition + index, segmentText);
                        }
                        else
                        {
                            Console.WriteLine("{0:D8}  Text: {1}", startPosition + index, segmentText);
                        }
                        index = index + (int)segmentSize;
                        //if ((index & 1) == 1)
                        //    index++;
                    }
                }
            }
        }

        private static void TryDumpFbmd(string fbmd)
        {
            var sb = new System.Text.StringBuilder("FBMD ");
            var ui = new System.Text.StringBuilder("             ");

            string prefix = fbmd.Substring(4, 2);
            sb.Append(prefix);
            sb.Append(" ");
            string counter = fbmd.Substring(6, 4);
            sb.Append(counter);
            sb.Append(" ");
            uint count = Convert.ToUInt32(counter, 16);
            for (int k = 0; k < count; k++)
            {
                string t = fbmd.Substring(10 + (8 * k), 8);
                sb.Append("0x");
                sb.Append(t);
                sb.Append(" ");

                t = t.Substring(6, 2) + t.Substring(4, 2) + t.Substring(2, 2) + t.Substring(0, 2);
                uint unk = Convert.ToUInt32(t, 16);
                ui.Append(unk.ToString("[00000000]"));
                ui.Append(" ");
            }

            Console.WriteLine("          {0}", sb.ToString());
            Console.WriteLine("          {0}", ui.ToString());

        }

        private static void TryDumpAsAdobeApp14(IReadOnlyList<byte> dataBuffer, long startPosition)
        {
            if (dataBuffer.Count != 5 + 2 + 2 + 2 + 1)
                return;

            // Check for 'Adobe'
            if (!(dataBuffer[0] == 'A' && dataBuffer[1] == 'd' && dataBuffer[2] == 'o' && dataBuffer[3] == 'b' && dataBuffer[4] == 'e'))
                return;

            Console.WriteLine("{0:D8}  APP14 'Adobe' identifier", startPosition);
            int index = 5;
            uint version = ConvertToUint16FromBigEndian(dataBuffer, index);
            Console.WriteLine("{0:D8}   Version {1}", startPosition + index, version);
            index += 6;
            Console.WriteLine("{0:D8}   ColorSpace {1} (0 = Unknown (monochrome or RGB), 1 = YCbCr, 2 = YCCK)", startPosition + index, dataBuffer[index]);
        }

        private void TryDumpAsHPColorSpace(IReadOnlyList<byte> dataBuffer)
        {
            if (dataBuffer.Count != 5)
                return;

            // Check for 'colr' stored in little endian
            if (!(dataBuffer[0] == 0x72 && dataBuffer[1] == 0x6C && dataBuffer[2] == 0x6F && dataBuffer[3] == 0x63))
                return;

            Console.WriteLine("{0:D8}  HP color space, defined by HP JPEG-LS implementation", GetStartOffset() - 3);
            Console.WriteLine("{0:D8}  Color Space = {1} ({2})", GetStartOffset(), dataBuffer[4], GetHPColorSpaceName(dataBuffer[4]));
        }

        private ushort ReadUInt16BigEndian()
        {
            return (ushort)((reader.ReadByte() << 8) | reader.ReadByte());
        }

        private uint ReadUInt24BigEndian()
        {
            return (ushort)((reader.ReadByte() << 16) | (reader.ReadByte() << 8) | reader.ReadByte());
        }
        private uint ReadUInt32BigEndian()
        {
            return (uint)((reader.ReadByte() << 24) | (reader.ReadByte() << 16) | (reader.ReadByte() << 8) | reader.ReadByte());
        }

        private static uint ConvertToUint32BigEndian(IReadOnlyList<byte> buffer, int index)
        {
            return (uint)((buffer[index] << 24) | (buffer[index + 1] << 16) | (buffer[index + 2] << 8) | buffer[index + 3]);
        }

        private static uint ConvertToUint16FromBigEndian(IReadOnlyList<byte> buffer, int index)
        {
            return (uint)((buffer[index] << 8) | buffer[index + 1]);
        }

        private static string GetInterleaveModeName(byte interleaveMode)
        {
            switch (interleaveMode)
            {
                case 0: return "None";
                case 1: return "Line interleaved";
                case 2: return "Sample interleaved";
                default: return "Invalid";
            };
        }

        private static string GetColorSpaceName(byte colorSpace)
        {
            switch (colorSpace)
            {
                case 0: return "Bi-level black";
                case 1: return "ITU-R BT.709 Video";
                case 2: return "None";
                case 3: return "ITU-R BT.601-1. (RGB)";
                case 4: return "ITU-R BT.601-1. (video)";
                case 8: return "Gray-scale";
                case 9: return "Photo CD™";
                case 10: return "RGB";
                case 11: return "CMY";
                case 12: return "CMYK";
                case 13: return "Transformed CMYK";
                case 14: return "CIE 1976(L * a * b *)";
                case 15: return "Bi-level white";
                default: return "Unknown";
            };
        }

        private static string GetCompressionTypeName(byte compressionType)
        {
            switch (compressionType)
            {
                case 0: return "Uncompressed";
                case 1: return "Modified Huffman";
                case 2: return "Modified READ";
                case 3: return "Modified Modified READ";
                case 4: return "ISO/IEC 11544 (JBIG)";
                case 5: return "ISO/IEC 10918-1 or ISO/IEC 10918-3 (JPEG)";
                case 6: return "ISO/IEC 14495-1 or ISO/IEC 14495-2 (JPEG-LS)";
                default: return "Unknown";
            };
        }

        private static string GetResolutionUnitsName(byte resolutionUnit)
        {
            switch (resolutionUnit)
            {
                case 0: return "Aspect Ratio";
                case 1: return "Dots per Inch";
                case 2: return "Dots per Centimeter";
                default: return "Unknown";
            };
        }

        private static string GetHPColorTransformationName(byte colorTransformation)
        {
            switch (colorTransformation)
            {
                case 1: return "HP1";
                case 2: return "HP2";
                case 3: return "HP3";
                case 4: return "RGB as YUV lossy";
                case 5: return "Matrix";
                default: return "Unknown";
            };
        }

        private static string GetHPColorSpaceName(byte colorSpace)
        {
            switch (colorSpace)
            {
                case 1: return "Gray";
                case 2: return "Palettized";
                case 3: return "RGB";
                case 4: return "YUV";
                case 5: return "HSV";
                case 6: return "HSB";
                case 7: return "HSL";
                case 8: return "LAB";
                case 9: return "CMYK";
                default: return "Unknown";
            };
        }
    }

        public class Program
    {
        private static void Main(string[] args)
        {

            if (args.Length < 1)
            {
                Console.WriteLine("Usage: jpegdump <filename>");
            }
            else
            {
                foreach (var arg in args)
                {

                    try
                    {
                        using (var stream = new FileStream(arg, FileMode.Open))
                        {
                            Console.WriteLine("=============================================================================");
                            Console.WriteLine("Dumping JPEG file: {0}", arg);
                            Console.WriteLine("=============================================================================");
                            using (var jpegStreamReader = new JpegStreamReader(stream))
                            {
                                jpegStreamReader.Dump();
                            }
                        }
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine("Failed to open \\ parse file {0}, error: ", arg, e.Message);
                    }
                }
            }
            System.Diagnostics.Debugger.Break();
        }
   }
}
