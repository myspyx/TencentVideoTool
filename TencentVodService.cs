using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TencentCloud.Common;
using TencentCloud.Common.Profile;
using TencentCloud.Vod.V20180717;
using TencentCloud.Vod.V20180717.Models;

namespace TencentVideoTool
{
    public class TencentVodService
    {
        private ulong _subAppId;
        private VodClient _client;

        public TencentVodService(string secretId, string secretKey, ulong subAppId)
        {
            _subAppId = subAppId;

            var cred = new Credential
            {
                SecretId = secretId,
                SecretKey = secretKey
            };

            var clientProfile = new ClientProfile();
            var httpProfile = new HttpProfile();
            httpProfile.Endpoint = ("vod.tencentcloudapi.com");
            clientProfile.HttpProfile = httpProfile;

            _client = new VodClient(cred, "", clientProfile);
        }

        public List<MediaClassInfo> GetAllMediaClassInfos()
        {
            var req = new DescribeAllClassRequest();
            req.SubAppId = _subAppId;
            var resp = _client.DescribeAllClassSync(req);
            return resp.ClassInfoSet.ToList();
        }

        public List<MediaInfo> GetMediaInfos(long? classId)
        {
            var req = new SearchMediaRequest();
            req.ClassIds = new long?[] {classId};
            req.SubAppId = _subAppId;
            req.Limit = 5000;
            var resp = _client.SearchMediaSync(req);
            return resp.MediaInfoSet.ToList();
        }

        public void ClearSubtitles(MediaInfo mediaInfo)
        {
            // detach subtitle
            var detachReq = new AttachMediaSubtitlesRequest();
            detachReq.FileId = mediaInfo.FileId;
            detachReq.Operation = "Detach";
            detachReq.AdaptiveDynamicStreamingDefinition = 10;
            detachReq.SubtitleIds = mediaInfo.SubtitleInfo?.SubtitleSet?.Select(sts => sts.Id).ToArray() ?? new string[] { };
            detachReq.SubAppId = _subAppId;
            _client.AttachMediaSubtitlesSync(detachReq);

            // remove subtitle
            var modifyReq = new ModifyMediaInfoRequest();
            modifyReq.FileId = mediaInfo.FileId;
            modifyReq.ClearSubtitles = 1; // 1 means remove all subtitles
            modifyReq.SubAppId = _subAppId;
            _client.ModifyMediaInfoSync(modifyReq);

            mediaInfo.SubtitleInfo = null;
        }

        public void AddSubtitle(MediaInfo mediaInfo, string vttSubtitle)
        {
            // add subtitle
            var addReq = new ModifyMediaInfoRequest();
            addReq.FileId = mediaInfo.FileId;
            var mediaSubtitleInput1 = new MediaSubtitleInput();
            mediaSubtitleInput1.Name = "English";
            mediaSubtitleInput1.Language = "en-US";
            mediaSubtitleInput1.Format = "vtt";
            mediaSubtitleInput1.Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(vttSubtitle));
            addReq.AddSubtitles = new MediaSubtitleInput[] {mediaSubtitleInput1};

            addReq.SubAppId = _subAppId;
            var addRes = _client.ModifyMediaInfoSync(addReq);

            // attach subtitle
            var attachReq = new AttachMediaSubtitlesRequest();
            attachReq.FileId = mediaInfo.FileId;
            attachReq.Operation = "Attach";
            attachReq.AdaptiveDynamicStreamingDefinition = 10;
            attachReq.SubtitleIds = addRes.AddedSubtitleSet.Select(ass => ass.Id).ToArray();
            attachReq.SubAppId = _subAppId;
            _client.AttachMediaSubtitlesSync(attachReq);

            mediaInfo.SubtitleInfo = new MediaSubtitleInfo()
            {
                SubtitleSet = mediaInfo.SubtitleInfo?.SubtitleSet?.Concat(addRes.AddedSubtitleSet).ToArray() ?? addRes.AddedSubtitleSet
            };
        }
    }
}