using SIMULTAN.Data.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    internal static class ChatUtils
    {
        #region BLOCK-CHAIN: Ganache 1.2.2
        public static string GetFixedAddressFor(SimUserRole _user_role)
        {
            switch (_user_role)
            {
                case SimUserRole.ADMINISTRATOR:
                    return @"0xEA8e38626b5D4fad44B974ee3538241b04c6B4D6";
                case SimUserRole.MODERATOR:
                    return @"0x27Da27427FE6d8C65764482407F1390Cdb44A0eb";
                case SimUserRole.ENERGY_NETWORK_OPERATOR:
                    return @"0xE46b9557D58D9Dac31f41F7D06Af70a420C9ade3";
                case SimUserRole.ENERGY_SUPPLIER:
                    return @"0x8F14AaC38Bf3Dc521057fd44432c53c3CeBd2Bb3";
                case SimUserRole.BUILDING_DEVELOPER:
                    return @"0x1B258Ab40a7F6F24F29a14C427c57306B8572346";
                case SimUserRole.BUILDING_OPERATOR:
                    return @"0x789c65319f23490459A7389AA83e08E7f8b58084";
                case SimUserRole.ARCHITECTURE:
                    return @"0x8Fb29bD5c86E6442b3661830E03e3F7Ca6C7495e";
                case SimUserRole.FIRE_SAFETY:
                    return @"0x5fCBB1A787717c091883CB80c5c946d0F75E6b37";
                case SimUserRole.BUILDING_PHYSICS:
                    return @"0xA0A4213d3C2Ff3B0436dB75BaCc9569c72a6a3aF";
                case SimUserRole.MEP_HVAC:
                    return @"0xE0abB03656fcD134e654bf2d58aBB3f901318b8e";
                case SimUserRole.PROCESS_MEASURING_CONTROL:
                case SimUserRole.BUILDING_CONTRACTOR:
                case SimUserRole.GUEST:
                default:
                    return string.Empty;
            }
        }

        public static SecureString GetFixedPasswordFor(SimUserRole _user_role)
        {
            switch (_user_role)
            {
                case SimUserRole.ADMINISTRATOR:
                    return ChatUtils.StringToSecureString(@"bdbe91dd450d2bd134a5c429eaa731aa66980dac0629effb8ec025a4403ac63f");
                case SimUserRole.MODERATOR:
                    return ChatUtils.StringToSecureString(@"593bc977f0cd456d64888dcbe401cafef3a739dbb89c86b93fdcd19e4c2d608b");
                case SimUserRole.ENERGY_NETWORK_OPERATOR:
                    return ChatUtils.StringToSecureString(@"807756bc824c8e77c7652b14846356a7d780591202cc6a43a17bbcd5135d4ee2");
                case SimUserRole.ENERGY_SUPPLIER:
                    return ChatUtils.StringToSecureString(@"f5a1ccbb80040f2c6f615b1468c59c49fd20ae470a5944995d9708111f367cfd");
                case SimUserRole.BUILDING_DEVELOPER:
                    return ChatUtils.StringToSecureString(@"db917b2e32c3c71e2cf305341e266bbef3931d5e07628b1cc639831f04de8ccf");
                case SimUserRole.BUILDING_OPERATOR:
                    return ChatUtils.StringToSecureString(@"643ae2a0eccfbad048feb7ca806426bc2b51a3906622f3f6c2332e0ebf537d70");
                case SimUserRole.ARCHITECTURE:
                    return ChatUtils.StringToSecureString(@"1780c9e75631bd4b24580938781565b8997187c37a8ab281b4a51e965b0b888e");
                case SimUserRole.FIRE_SAFETY:
                    return ChatUtils.StringToSecureString(@"507e5bd5b092873f9070884b35a8cac1135e9b9d76aaa7b6b1e20e7b61b35c0c");
                case SimUserRole.BUILDING_PHYSICS:
                    return ChatUtils.StringToSecureString(@"7a26fd5cff651609c00192b10b7d5df221ef1ec7de3afc5be885990b1850c71a");
                case SimUserRole.MEP_HVAC:
                    return ChatUtils.StringToSecureString(@"b6f55bc21c29a586b69d25099a7dd5d0ea2fba4bdcf7e336101f22885f666ba1");
                case SimUserRole.PROCESS_MEASURING_CONTROL:
                case SimUserRole.BUILDING_CONTRACTOR:
                case SimUserRole.GUEST:
                default:
                    return new SecureString();
            }
        }

        #endregion

        #region SECURITY
        public static SecureString StringToSecureString(string _input)
        {
            char[] chars = _input.ToCharArray();
            SecureString psw = new SecureString();
            foreach (char c in chars)
            {
                psw.AppendChar(c);
            }
            return psw;
        }

        /// <summary>
        /// Source: https://stackoverflow.com/questions/818704/how-to-convert-securestring-to-system-string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        #endregion
    }
}
