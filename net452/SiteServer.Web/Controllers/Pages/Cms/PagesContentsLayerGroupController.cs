﻿using System;
using System.Web.Http;
using SiteServer.CMS.Caches;
using SiteServer.CMS.Caches.Content;
using SiteServer.CMS.Core;
using SiteServer.CMS.Database.Core;
using SiteServer.CMS.Database.Models;
using SiteServer.CMS.Plugin.Impl;
using SiteServer.Plugin;
using SiteServer.Utils;

namespace SiteServer.API.Controllers.Pages.Cms
{
    [RoutePrefix("pages/cms/contentsLayerGroup")]
    public class PagesContentsLayerGroupController : ApiController
    {
        private const string Route = "";

        [HttpGet, Route(Route)]
        public IHttpActionResult GetConfig()
        {
            try
            {
                var rest = Request.GetAuthenticatedRequest();

                var siteId = Request.GetQueryInt("siteId");
                var channelId = Request.GetQueryInt("channelId");

                if (!rest.IsAdminLoggin ||
                    !rest.AdminPermissions.HasChannelPermissions(siteId, channelId,
                        ConfigManager.ChannelPermissions.ContentDelete))
                {
                    return Unauthorized();
                }

                var siteInfo = SiteManager.GetSiteInfo(siteId);
                if (siteInfo == null) return BadRequest("无法确定内容对应的站点");

                var channelInfo = ChannelManager.GetChannelInfo(siteId, channelId);
                if (channelInfo == null) return BadRequest("无法确定内容对应的栏目");

                var contentGroupNameList = ContentGroupManager.GetGroupNameList(siteId);

                return Ok(new
                {
                    Value = contentGroupNameList
                });
            }
            catch (Exception ex)
            {
                LogUtils.AddErrorLog(ex);
                return InternalServerError(ex);
            }
        }

        [HttpPost, Route(Route)]
        public IHttpActionResult Submit()
        {
            try
            {
                var rest = Request.GetAuthenticatedRequest();

                var siteId = Request.GetPostInt("siteId");
                var channelId = Request.GetPostInt("channelId");
                var contentIdList = TranslateUtils.StringCollectionToIntList(Request.GetPostString("contentIds"));
                var pageType = Request.GetPostString("pageType");
                var groupNames = TranslateUtils.StringCollectionToStringList(Request.GetPostString("groupNames"));
                var groupName = Request.GetPostString("groupName");
                var description = Request.GetPostString("description");

                if (!rest.IsAdminLoggin ||
                    !rest.AdminPermissions.HasChannelPermissions(siteId, channelId,
                        ConfigManager.ChannelPermissions.ContentEdit))
                {
                    return Unauthorized();
                }

                var siteInfo = SiteManager.GetSiteInfo(siteId);
                if (siteInfo == null) return BadRequest("无法确定内容对应的站点");

                var channelInfo = ChannelManager.GetChannelInfo(siteId, channelId);
                if (channelInfo == null) return BadRequest("无法确定内容对应的栏目");

                if (pageType == "setGroup")
                {
                    foreach (var contentId in contentIdList)
                    {
                        var contentInfo = ContentManager.GetContentInfo(siteInfo, channelInfo, contentId);
                        if (contentInfo == null) continue;

                        var list = TranslateUtils.StringCollectionToStringList(contentInfo.GroupNameCollection);
                        foreach (var name in groupNames)
                        {
                            if (!list.Contains(name)) list.Add(name);
                        }
                        contentInfo.GroupNameCollection = TranslateUtils.ObjectCollectionToString(list);

                        DataProvider.ContentRepository.Update(siteInfo, channelInfo, contentInfo);
                    }

                    LogUtils.AddSiteLog(siteId, rest.AdminName, "批量设置内容组", $"内容组:{TranslateUtils.ObjectCollectionToString(groupNames)}");
                }
                else if(pageType == "cancelGroup")
                {
                    foreach (var contentId in contentIdList)
                    {
                        var contentInfo = ContentManager.GetContentInfo(siteInfo, channelInfo, contentId);
                        if (contentInfo == null) continue;

                        var list = TranslateUtils.StringCollectionToStringList(contentInfo.GroupNameCollection);
                        foreach (var name in groupNames)
                        {
                            if (list.Contains(name)) list.Remove(name);
                        }
                        contentInfo.GroupNameCollection = TranslateUtils.ObjectCollectionToString(list);

                        DataProvider.ContentRepository.Update(siteInfo, channelInfo, contentInfo);
                    }

                    LogUtils.AddSiteLog(siteId, rest.AdminName, "批量取消内容组", $"内容组:{TranslateUtils.ObjectCollectionToString(groupNames)}");
                }
                else if (pageType == "addGroup")
                {
                    var groupInfo = new ContentGroupInfo
                    {
                        GroupName = AttackUtils.FilterXss(groupName),
                        SiteId = siteId,
                        Description = AttackUtils.FilterXss(description)
                    };

                    if (ContentGroupManager.IsExists(siteId, groupInfo.GroupName))
                    {
                        DataProvider.ContentGroup.Update(groupInfo);
                        LogUtils.AddSiteLog(siteId, rest.AdminName, "修改内容组", $"内容组:{groupInfo.GroupName}");
                    }
                    else
                    {
                        DataProvider.ContentGroup.Insert(groupInfo);
                        LogUtils.AddSiteLog(siteId, rest.AdminName, "添加内容组", $"内容组:{groupInfo.GroupName}");
                    }

                    foreach (var contentId in contentIdList)
                    {
                        var contentInfo = ContentManager.GetContentInfo(siteInfo, channelInfo, contentId);
                        if (contentInfo == null) continue;

                        var list = TranslateUtils.StringCollectionToStringList(contentInfo.GroupNameCollection);
                        if (!list.Contains(groupInfo.GroupName)) list.Add(groupInfo.GroupName);
                        contentInfo.GroupNameCollection = TranslateUtils.ObjectCollectionToString(list);

                        DataProvider.ContentRepository.Update(siteInfo, channelInfo, contentInfo);
                    }

                    LogUtils.AddSiteLog(siteId, rest.AdminName, "批量设置内容组", $"内容组:{groupInfo.GroupName}");
                }

                return Ok(new
                {
                    Value = contentIdList
                });
            }
            catch (Exception ex)
            {
                LogUtils.AddErrorLog(ex);
                return InternalServerError(ex);
            }
        }
    }
}
