﻿using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using Newtonsoft.Json;
using WLC.Admin.Infrastructure.Concrete;
using WLC.Domain.Entities;
using WLC.Domain.Interface;

namespace WLC.Admin.Controllers
{
    [Authorize]
    [SessionExpireFilter]
    public class DefaultController : Controller
    {
        private IWLCTanimRepo wlcTanimRepo;
        public DefaultController(IWLCTanimRepo wtr)
        {
            wlcTanimRepo = wtr;
        }

        public ActionResult Index()
        {
            if (Session["CurrentUserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            else
                return View();
        }

        public ViewResult Liste()
        {
            return View();
        }

        public string JsonSorgulaList()
        {
            var iDisplayLength = int.Parse(Request["iDisplayLength"]);
            var iDisplayStart = int.Parse(Request["iDisplayStart"]);
            var iSearch = Request["sSearch"];
            var iSortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
            var iSortDirection = Request["sSortDir_0"];
            var totalRecords = wlcTanimRepo.WLCTanimlar.Count();

            if (iDisplayLength == -1)
            {
                iDisplayLength = totalRecords;
            }

            var filteredList = wlcTanimRepo.WLCTanimlar;
            if (!string.IsNullOrEmpty(iSearch))
            {
                var search = iSearch.ToLower();
                filteredList = wlcTanimRepo.WLCTanimlar.Where(x => (!x.APSAYISI.IsEmpty() && x.APSAYISI.ToLower().Contains(search)) ||
                                                   (!x.FLEXCONNAME.IsEmpty() && x.FLEXCONNAME.ToLower().Contains(search)) ||
                                                   (!x.IL.IsEmpty() && x.IL.ToLower().Contains(search)) ||
                                                   (!x.ILCE.IsEmpty() && x.ILCE.ToLower().Contains(search)) ||
                                                   (!x.IP.IsEmpty() && x.IP.ToLower().Contains(search)) ||
                                                   (!x.OKULADI.IsEmpty() && x.OKULADI.ToLower().Contains(search)) ||
                                                   (!x.OKULKODU.IsEmpty() && x.OKULKODU.ToLower().Contains(search)) ||
                                                   (!x.SUBNET.IsEmpty() && x.SUBNET.ToLower().Contains(search)) ||
                                                   (!x.TESISKODU.IsEmpty() && x.TESISKODU.ToLower().Contains(search)) ||
                                                   (!x.WLC1IP.IsEmpty() && x.WLC1IP.ToLower().Contains(search)) ||
                                                   (!x.WLC1NAME.IsEmpty() && x.WLC1NAME.ToLower().Contains(search)) ||
                                                   (!x.WLC2IP.IsEmpty() && x.WLC2IP.ToLower().Contains(search)) ||
                                                   (!x.WLC2NAME.IsEmpty() && x.WLC2NAME.ToLower().Contains(search)) ||
                                                   (!x.SERVERNAME.IsEmpty() && x.SERVERNAME.ToLower().Contains(search)));
            }

            Func<WLCTanim, string> orderFunc = (item => iSortColumnIndex == 1
                ? item.IL
                : iSortColumnIndex == 2
                    ? item.ILCE
                    : iSortColumnIndex == 3
                        ? item.OKULADI
                        : iSortColumnIndex == 4
                            ? item.OKULKODU
                            : iSortColumnIndex == 5
                                ? item.APSAYISI
                                : iSortColumnIndex == 6
                                    ? item.WLC1NAME
                                    : iSortColumnIndex == 7
                                        ? item.WLC1IP
                                        : iSortColumnIndex == 8
                                            ? item.WLC2IP
                                            : iSortColumnIndex == 9
                                                ? item.WLC2NAME
                                                : iSortColumnIndex == 10
                                                    ? item.SERVERNAME
                                                    : iSortColumnIndex == 11
                                                        ? item.FLEXCONNAME
                                                        : iSortColumnIndex == 12
                                                            ? item.TESISKODU
                                                            : iSortColumnIndex == 13
                                                                ? item.IP
                                                                : item.SUBNET);

            var orderedList = (iSortDirection == "asc") ? filteredList.OrderBy(orderFunc).ToList() : filteredList.OrderByDescending(orderFunc).ToList();
            var list = orderedList.Skip(iDisplayStart).Take(iDisplayLength);

            var result = new
            {
                iTotalRecords = totalRecords,
                iTotalDisplayRecords = filteredList.Count(),
                aaData = (from item in list
                          select new[] 
                            {
                                Convert.ToString(item.ID), 
                                item.IL,
                                item.ILCE,
                                item.OKULADI,
                                item.OKULKODU,
                                item.APSAYISI,
                                item.WLC1NAME,
                                item.WLC1IP,
                                item.WLC2NAME,
                                item.WLC2IP,
                                item.SERVERNAME,
                                item.FLEXCONNAME,
                                item.TESISKODU,
                                item.IP,
                                item.SUBNET,
                                Convert.ToString(item.DONE),
                                ""
                            })
            };

            return JsonConvert.SerializeObject(result);
        }

        [HttpPost]
        public string Isle(string id)
        {
            var kullanici = Session["CurrentUserName"].ToString();
            var status = wlcTanimRepo.WLCKaydet(id, kullanici);
            var result = new
            {
                Status = status.ToString()
            };
            return JsonConvert.SerializeObject(result);
        }

        public ViewResult Rapor()
        {
            return View();
        }

        public string RaporGunlukAp()
        {
            var iDisplayLength = int.Parse(Request["iDisplayLength"]);
            var iDisplayStart = int.Parse(Request["iDisplayStart"]);
            var iSortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
            var iSortDirection = Request["sSortDir_0"];

            var doneWlcList = wlcTanimRepo.WLCTanimlar.Where(x => x.DONE.Equals(true) && x.TARIH.HasValue);
            var groupedList = (from wlc in doneWlcList
                               group wlc by wlc.TARIH.Value.ToShortDateString()
                                   into wlcGrouped
                                   select new Tuple<string, int>(
                                       wlcGrouped.Key,
                                       wlcGrouped.Sum(x => Convert.ToInt32(x.APSAYISI))
                                       ));

            var totalRecords = groupedList.Count();
            if (iDisplayLength == -1)
            {
                iDisplayLength = totalRecords;
            }

            Func<Tuple<string, int>, string> orderFunc = (item => iSortColumnIndex == 0 ? item.Item1 : item.Item2.ToString());

            var orderedList = (iSortDirection == "asc") ? groupedList.OrderBy(orderFunc).ToList() : groupedList.OrderByDescending(orderFunc).ToList();
            var list = orderedList.Skip(iDisplayStart).Take(iDisplayLength);

            var result = new
            {
                iTotalRecords = wlcTanimRepo.WLCTanimlar.Count(),
                iTotalDisplayRecords = groupedList.Count(),
                aaData = (from item in list
                          select new[]
                          {
                              item.Item1, 
                              item.Item2.ToString() 
                          })
            };
            return JsonConvert.SerializeObject(result);
        }

        public string RaporGunlukKullaniciAp()
        {
            var iDisplayLength = int.Parse(Request["iDisplayLength"]);
            var iDisplayStart = int.Parse(Request["iDisplayStart"]);
            var iSortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
            var iSortDirection = Request["sSortDir_0"];

            var doneWlcList = wlcTanimRepo.WLCTanimlar.Where(x => x.DONE.Equals(true) && x.TARIH.HasValue);
            var groupedList = (from wlc in doneWlcList
                               group wlc by new
                               {
                                   TARIH = wlc.TARIH.Value.ToShortDateString(),
                                   wlc.KULLANICI
                               } into wlcGrouped
                               select new Tuple<string, string, string>(
                                   wlcGrouped.Key.TARIH,
                                   wlcGrouped.Key.KULLANICI,
                                   wlcGrouped.Sum(x => Convert.ToInt32(x.APSAYISI)).ToString()
                               ));

            var totalRecords = groupedList.Count();
            if (iDisplayLength == -1)
            {
                iDisplayLength = totalRecords;
            }

            Func<Tuple<string, string, string>, string> orderFunc = (item => iSortColumnIndex == 0
                ? item.Item1
                : iSortColumnIndex == 1
                    ? item.Item2
                    : item.Item3);

            var orderedList = (iSortDirection == "asc") ? groupedList.OrderBy(orderFunc).ToList() : groupedList.OrderByDescending(orderFunc).ToList();
            var list = orderedList.Skip(iDisplayStart).Take(iDisplayLength);

            var result = new
            {
                iTotalRecords = wlcTanimRepo.WLCTanimlar.Count(),
                iTotalDisplayRecords = groupedList.Count(),
                aaData = (from item in list
                          select new[] 
                          { 
                              item.Item1, 
                              item.Item2, 
                              item.Item3
                          })
            };
            return JsonConvert.SerializeObject(result);
        }

        public string RaporToplamAp()
        {
            var toplamAp = wlcTanimRepo.WLCTanimlar.Sum(x => Convert.ToInt32(x.APSAYISI));
            var yapilanAp = wlcTanimRepo.WLCTanimlar.Where(x => x.DONE.Equals(true)).Sum(x => Convert.ToInt32(x.APSAYISI));
            var result = new
            {
                iTotalRecords = wlcTanimRepo.WLCTanimlar.Count(),
                iTotalDisplayRecords = 1,
                aaData = new[] { new[] { 
                    toplamAp,
                    yapilanAp,
                    toplamAp - yapilanAp
                }}
            };
            return JsonConvert.SerializeObject(result);
        }

        public string RaporToplamIlAp()
        {
            var iDisplayLength = int.Parse(Request["iDisplayLength"]);
            var iDisplayStart = int.Parse(Request["iDisplayStart"]);
            var iSortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
            var iSortDirection = Request["sSortDir_0"];

            var groupedList = (from wlc in wlcTanimRepo.WLCTanimlar
                               group wlc by wlc.IL into wlcGrouped
                               select new Tuple<string, string, string, string>(
                                   wlcGrouped.Key,
                                   wlcGrouped.Sum(x => Convert.ToInt32(x.APSAYISI)).ToString(),
                                   wlcGrouped.Where(x => x.DONE.Equals(true)).Sum(x => Convert.ToInt32(x.APSAYISI)).ToString(),
                                   (wlcGrouped.Sum(x => Convert.ToInt32(x.APSAYISI)) - wlcGrouped.Where(x => x.DONE.Equals(true)).Sum(x => Convert.ToInt32(x.APSAYISI))).ToString()
                               ));

            var totalRecords = groupedList.Count();
            if (iDisplayLength == -1)
            {
                iDisplayLength = totalRecords;
            }

            Func<Tuple<string, string, string, string>, string> orderFunc = (item => iSortColumnIndex == 0
                ? item.Item1
                : iSortColumnIndex == 1
                    ? item.Item2
                    : iSortColumnIndex == 2
                        ? item.Item3
                        : item.Item4);

            var orderedList = (iSortDirection == "asc") ? groupedList.OrderBy(orderFunc).ToList() : groupedList.OrderByDescending(orderFunc).ToList();
            var list = orderedList.Skip(iDisplayStart).Take(iDisplayLength);

            var result = new
            {
                iTotalRecords = wlcTanimRepo.WLCTanimlar.Count(),
                iTotalDisplayRecords = groupedList.Count(),
                aaData = (from item in list
                          select new[] 
                          { 
                              item.Item1, 
                              item.Item2, 
                              item.Item3,
                              item.Item4
                          })
            };
            return JsonConvert.SerializeObject(result);
        }

        public string RaporToplamIlceAp()
        {
            var iDisplayLength = int.Parse(Request["iDisplayLength"]);
            var iDisplayStart = int.Parse(Request["iDisplayStart"]);
            var iSortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
            var iSortDirection = Request["sSortDir_0"];

            var groupedList = (from wlc in wlcTanimRepo.WLCTanimlar
                               group wlc by new { wlc.IL, wlc.ILCE } into wlcGrouped
                               select new Tuple<string, string, string, string>(
                                   wlcGrouped.Key.IL + "-" + wlcGrouped.Key.ILCE,
                                   wlcGrouped.Sum(x => Convert.ToInt32(x.APSAYISI)).ToString(),
                                   wlcGrouped.Where(x => x.DONE.Equals(true)).Sum(x => Convert.ToInt32(x.APSAYISI)).ToString(),
                                   (wlcGrouped.Sum(x => Convert.ToInt32(x.APSAYISI)) - wlcGrouped.Where(x => x.DONE.Equals(true)).Sum(x => Convert.ToInt32(x.APSAYISI))).ToString()
                               ));

            var totalRecords = groupedList.Count();
            if (iDisplayLength == -1)
            {
                iDisplayLength = totalRecords;
            }

            Func<Tuple<string, string, string, string>, string> orderFunc = (item => iSortColumnIndex == 0
                ? item.Item1
                : iSortColumnIndex == 1
                    ? item.Item2
                    : iSortColumnIndex == 2
                        ? item.Item3
                        : item.Item4);

            var orderedList = (iSortDirection == "asc") ? groupedList.OrderBy(orderFunc).ToList() : groupedList.OrderByDescending(orderFunc).ToList();
            var list = orderedList.Skip(iDisplayStart).Take(iDisplayLength);

            var result = new
            {
                iTotalRecords = wlcTanimRepo.WLCTanimlar.Count(),
                iTotalDisplayRecords = groupedList.Count(),
                aaData = (from item in list
                          select new[] 
                          { item.Item1, 
                              item.Item2, 
                              item.Item3,
                              item.Item4
                          })
            };
            return JsonConvert.SerializeObject(result);
        }

        public string RaporToplamOkulAp()
        {
            var iDisplayLength = int.Parse(Request["iDisplayLength"]);
            var iDisplayStart = int.Parse(Request["iDisplayStart"]);
            var iSortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
            var iSortDirection = Request["sSortDir_0"];

            var groupedList = (from wlc in wlcTanimRepo.WLCTanimlar
                               group wlc by new
                               {
                                   wlc.OKULADI,
                                   wlc.IL,
                                   wlc.ILCE,
                                   wlc.OKULKODU
                               } into wlcGrouped
                               select new Tuple<string, string, string, string, string, string>(
                                   wlcGrouped.Key.OKULADI,
                                   wlcGrouped.Key.IL + "-" + wlcGrouped.Key.ILCE,
                                   wlcGrouped.Key.OKULKODU,
                                   wlcGrouped.Sum(x => Convert.ToInt32(x.APSAYISI)).ToString(),
                                   wlcGrouped.Where(x => x.DONE.Equals(true)).Sum(x => Convert.ToInt32(x.APSAYISI)).ToString(),
                                   (wlcGrouped.Sum(x => Convert.ToInt32(x.APSAYISI)) - wlcGrouped.Where(x => x.DONE.Equals(true)).Sum(x => Convert.ToInt32(x.APSAYISI))).ToString()
                               ));

            var totalRecords = groupedList.Count();
            if (iDisplayLength == -1)
            {
                iDisplayLength = totalRecords;
            }

            Func<Tuple<string, string, string, string, string, string>, string> orderFunc =
                (item => iSortColumnIndex == 0
                    ? item.Item1
                    : iSortColumnIndex == 1
                        ? item.Item2
                        : iSortColumnIndex == 2
                            ? item.Item3
                            : iSortColumnIndex == 3
                                ? item.Item4
                                : iSortColumnIndex == 4
                                    ? item.Item5
                                    : item.Item6);

            var orderedList = (iSortDirection == "asc") ? groupedList.OrderBy(orderFunc).ToList() : groupedList.OrderByDescending(orderFunc).ToList();
            var list = orderedList.Skip(iDisplayStart).Take(iDisplayLength);

            var result = new
            {
                iTotalRecords = totalRecords,
                iTotalDisplayRecords = groupedList.Count(),
                aaData = (from item in list
                          select new[] 
                          { 
                              item.Item1,
                              item.Item2,
                              item.Item3,
                              item.Item4, 
                              item.Item5,
                              item.Item6
                          })
            };
            return JsonConvert.SerializeObject(result);
        }
    }
}