﻿using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Remotely.Server.Data;
using Remotely.Server.Services;
using Remotely.Shared.Models;
using Remotely.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remotely.Tests
{
    [TestClass]
    public class DataServiceTests
    {
        private DataService DataService { get; set; }

        [TestInitialize]
        public async Task TestInit()
        {
            await TestData.PopulateTestData();
            DataService = IoCActivator.ServiceProvider.GetRequiredService<DataService>();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestData.ClearData();
        }

        [TestMethod]
        [DoNotParallelize]
        public void VerifyInitialData()
        {
            Assert.IsNotNull(DataService.GetUserByName(TestData.Admin1.UserName));
            Assert.IsNotNull(DataService.GetUserByName(TestData.Admin2.UserName));
            Assert.IsNotNull(DataService.GetUserByName(TestData.User1.UserName));
            Assert.IsNotNull(DataService.GetUserByName(TestData.User2.UserName));
            Assert.AreEqual(1, DataService.GetOrganizationCount());

            var devices = DataService.GetAllDevices(TestData.OrganizationID);

            Assert.AreEqual(2, devices.Count());
            Assert.IsTrue(devices.Any(x => x.ID == "Device1"));
            Assert.IsTrue(devices.Any(x => x.ID == "Device2"));

            var orgIDs = new string[]
            {
                TestData.Group1.OrganizationID,
                TestData.Group2.OrganizationID,
                TestData.Admin1.OrganizationID,
                TestData.Admin2.OrganizationID,
                TestData.User1.OrganizationID,
                TestData.User2.OrganizationID,
                TestData.Device1.OrganizationID,
                TestData.Device2.OrganizationID
            };

            Assert.IsTrue(orgIDs.All(x => x == TestData.OrganizationID));
        }


        [TestMethod]
        [DoNotParallelize]
        public void UpdateOrganizationName()
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(TestData.Admin1.Organization.OrganizationName));
            DataService.UpdateOrganizationName(TestData.OrganizationID, "Test Org");
            Assert.AreEqual(TestData.Admin1.Organization.OrganizationName, "Test Org");
        }


        [TestMethod]
        [DoNotParallelize]
        public void DeviceGroupPermissions()
        {
            Assert.IsTrue(DataService.GetDevicesForUser(TestData.Admin1.UserName).Count() == 2);
            Assert.IsTrue(DataService.GetDevicesForUser(TestData.Admin2.UserName).Count() == 2);
            Assert.IsTrue(DataService.GetDevicesForUser(TestData.User1.UserName).Count() == 2);
            Assert.IsTrue(DataService.GetDevicesForUser(TestData.User2.UserName).Count() == 2);

            var groupID = DataService.GetDeviceGroups(TestData.Admin1.UserName).First().ID;

            DataService.UpdateDevice(TestData.Device1.ID, "", "", groupID);
            DataService.AddUserToDeviceGroup(TestData.OrganizationID, groupID, TestData.User1.UserName, out _);

            Assert.IsTrue(DataService.GetDevicesForUser(TestData.Admin1.UserName).Count() == 2);
            Assert.IsTrue(DataService.GetDevicesForUser(TestData.Admin2.UserName).Count() == 2);
            Assert.IsTrue(DataService.GetDevicesForUser(TestData.User1.UserName).Count() == 2);
            Assert.IsTrue(DataService.GetDevicesForUser(TestData.User2.UserName).Count() == 1);

            Assert.IsTrue(DataService.DoesUserHaveAccessToDevice(TestData.Device1.ID, TestData.Admin1));
            Assert.IsTrue(DataService.DoesUserHaveAccessToDevice(TestData.Device1.ID, TestData.Admin2));
            Assert.IsTrue(DataService.DoesUserHaveAccessToDevice(TestData.Device1.ID, TestData.User1));
            Assert.IsFalse(DataService.DoesUserHaveAccessToDevice(TestData.Device1.ID, TestData.User2));

            var allDevices = DataService.GetAllDevices(TestData.OrganizationID).Select(x => x.ID).ToArray();

            Assert.AreEqual(2, DataService.FilterDeviceIDsByUserPermission(allDevices, TestData.Admin1).Count());
            Assert.AreEqual(2, DataService.FilterDeviceIDsByUserPermission(allDevices, TestData.Admin2).Count());
            Assert.AreEqual(2, DataService.FilterDeviceIDsByUserPermission(allDevices, TestData.User1).Count());
            Assert.AreEqual(1, DataService.FilterDeviceIDsByUserPermission(allDevices, TestData.User2).Count());
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task UpdateDevice()
        {
            var newDevice = await DeviceInformation.Create("Device1", TestData.OrganizationID);
            Assert.IsTrue(DataService.AddOrUpdateDevice(newDevice, out _));
            Assert.AreEqual(TestData.Device1.OrganizationID, TestData.OrganizationID);
            Assert.AreEqual(TestData.Device1.DeviceName, Environment.MachineName);
            Assert.IsTrue(TestData.Device1.CpuUtilization > 0);
            Assert.IsTrue(TestData.Device1.TotalMemory > 0);
            Assert.IsTrue(TestData.Device1.TotalStorage > 0);
            Assert.IsTrue(TestData.Device1.UsedMemory > 0);
            Assert.IsTrue(TestData.Device1.UsedStorage > 0);
            Assert.IsTrue(TestData.Device1.IsOnline);
            Assert.AreEqual(Environment.Is64BitOperatingSystem, TestData.Device1.Is64Bit);
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task UpdateServerAdmins()
        {
            var currentAdmins = DataService.GetServerAdmins();
            Assert.AreEqual(1, currentAdmins.Count);
            var newAdmins = new List<string>()
            {
                TestData.Admin2.UserName
            };

            await DataService.UpdateServerAdmins(newAdmins, TestData.Admin1.UserName);

            currentAdmins = DataService.GetServerAdmins();
            Assert.AreEqual(2, currentAdmins.Count);
            Assert.IsTrue(currentAdmins.Contains(TestData.Admin1.UserName));
            Assert.IsTrue(currentAdmins.Contains(TestData.Admin2.UserName));

            await DataService.UpdateServerAdmins(newAdmins, TestData.Admin2.UserName);
            currentAdmins = DataService.GetServerAdmins();
            Assert.AreEqual(1, currentAdmins.Count);
            Assert.AreEqual(TestData.Admin2.UserName, currentAdmins[0]);
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task AddAlert()
        {
            var alert = new Alert()
            {
                DeviceID = TestData.Device1.ID,
                OrganizationID = TestData.OrganizationID,
                Message = "Test Message",
                UserID = TestData.Admin1.Id
            };
            await DataService.AddAlert(alert);
        }
    }
}
