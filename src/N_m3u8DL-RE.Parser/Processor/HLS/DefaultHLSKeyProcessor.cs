﻿using N_m3u8DL_RE.Common.Entity;
using N_m3u8DL_RE.Common.Enum;
using N_m3u8DL_RE.Common.Log;
using N_m3u8DL_RE.Common.Resource;
using N_m3u8DL_RE.Common.Util;
using N_m3u8DL_RE.Parser.Config;
using N_m3u8DL_RE.Parser.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace N_m3u8DL_RE.Parser.Processor.HLS
{
    public class DefaultHLSKeyProcessor : KeyProcessor
    {
        public override bool CanProcess(ExtractorType extractorType, string m3u8Url, string keyLine, string m3u8Content, ParserConfig paserConfig) => extractorType == ExtractorType.HLS;


        public override EncryptInfo Process(string keyLine, string m3u8Url, string m3u8Content, ParserConfig parserConfig)
        {
            var iv = ParserUtil.GetAttribute(keyLine, "IV");
            var method = ParserUtil.GetAttribute(keyLine, "METHOD");
            var uri = ParserUtil.GetAttribute(keyLine, "URI");

            Logger.Debug("METHOD:{},URI:{},IV:{}", method, uri, iv);

            var encryptInfo = new EncryptInfo(method);
            //IV
            if (!string.IsNullOrEmpty(iv))
            {
                encryptInfo.IV = HexUtil.HexToBytes(iv);
            }

            //KEY
            try
            {
                if (uri.ToLower().StartsWith("base64:"))
                {
                    encryptInfo.Key = Convert.FromBase64String(uri[7..]);
                }
                else if (uri.ToLower().StartsWith("data:;base64,"))
                {
                    encryptInfo.Key = Convert.FromBase64String(uri[13..]);
                }
                else if (uri.ToLower().StartsWith("data:text/plain;base64,"))
                {
                    encryptInfo.Key = Convert.FromBase64String(uri[23..]);
                }
                else if (!string.IsNullOrEmpty(uri))
                {
                    var segUrl = PreProcessUrl(ParserUtil.CombineURL(m3u8Url, uri), parserConfig);
                    var bytes = HTTPUtil.GetBytesAsync(segUrl, parserConfig.Headers).Result;
                    encryptInfo.Key = bytes;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ResString.cmd_loadKeyFailed + ": " + ex.Message);
                encryptInfo.Method = EncryptMethod.UNKNOWN;
            }

            return encryptInfo;
        }

        /// <summary>
        /// 预处理URL
        /// </summary>
        private string PreProcessUrl(string url, ParserConfig parserConfig)
        {
            foreach (var p in parserConfig.UrlProcessors)
            {
                if (p.CanProcess(ExtractorType.HLS, url, parserConfig))
                {
                    url = p.Process(url, parserConfig);
                }
            }

            return url;
        }
    }
}
