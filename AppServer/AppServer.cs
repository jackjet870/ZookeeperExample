﻿using System;
using System.Collections.Generic;
using System.Threading;
using Org.Apache.Zookeeper.Data;
using ZooKeeperNet;

namespace AppServer
{
    public class AppServer : IDisposable
    {
        private static readonly object LockObject = new object();
        private static readonly Lazy<AppServer> lazy =
            new Lazy<AppServer>(() => new AppServer());

        public static AppServer Instance
        {
            get { return lazy.Value; }
        }

        private ZooKeeper _zk = Singleton.Instance.ZkClient();

        public void Register(string address,bool isInit=false)
        {
            lock (LockObject)
            {
                var groupNode = Singleton.Instance.GroupNode();
                var subNode = Singleton.Instance.SubNode();
                var zk = Singleton.Instance.ZkClient();
                var statGroupNode = IsExistsGroupNode(groupNode);
                if (statGroupNode == null) CreateGroupNode(groupNode);
                if (isInit)
                {
                    Console.WriteLine("节点{0}：初始化", address);
                    CreateSubNode(address, groupNode, subNode);
                    return;
                }
                var statSubNode = IsExistSubNode(groupNode, address);             
                if (!statSubNode) CreateSubNode(address, groupNode, subNode);
                if (statSubNode)
                {
                    Console.WriteLine("节点{0}：已存在",address);
                }
            }

        }

        private bool IsExistSubNode(string groupNode,string address)
        {
            // 获取并监听groupNode的子节点变化
            // watch参数为true, 表示监听子节点变化事件. 
            // 每次都需要重新注册监听, 因为一次注册, 只能监听一次事件, 如果还想继续保持监听, 必须重新注册
            var subList = GetChildren(groupNode);
            foreach (var subNode in subList)
            {
                // 获取每个子节点下关联的server地址
                byte[] data = GetSubNodeData(groupNode, subNode);
                //跳过NoNodeException的异常情况
                if (data == null) continue;
                string val = System.Text.Encoding.Default.GetString(data);
                //如果节点已经注册，就不再重新注册
                if (val.Equals(address)) return true;
            }
            return false;
        }
        private IEnumerable<string> GetChildren(string groupNode)
        {
            while (true)
            {
                try
                {
                    // watch参数为true, 表示监听子节点变化事件.
                    // 每次都需要重新注册监听, 因为一次注册, 只能监听一次事件, 如果还想继续保持监听, 必须重新注册
                    //--服务器端主要是注册服务，所以不需要监听变化--false
                    return _zk.GetChildren("/" + groupNode, false);
                }
                //ConnectionLossException-Sleep 1秒
                catch (KeeperException.ConnectionLossException)
                {
                    Thread.Sleep(1000);
                }
                catch (KeeperException.SessionExpiredException)
                {
                    Dispose();
                    _zk = Singleton.Instance.ZkClient();
                    Thread.Sleep(1000);
                }
                catch (KeeperException.OperationTimeoutException)
                {
                    Dispose();
                    _zk = Singleton.Instance.ZkClient();
                    Thread.Sleep(1000);
                }
                catch (TimeoutException)
                {
                    Dispose();
                    _zk = Singleton.Instance.ZkClient();
                    Thread.Sleep(1000);
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }
        private byte[] GetSubNodeData(string groupNode, string subNode)
        {
            while (true)
            {
                try
                {
                    return _zk.GetData("/" + groupNode + "/" + subNode, false, null);
                }
                //ConnectionLossException-Sleep 1秒
                catch (KeeperException.ConnectionLossException)
                {
                    Thread.Sleep(1000);
                }
                catch (KeeperException.SessionExpiredException)
                {
                    Dispose();
                    _zk = Singleton.Instance.ZkClient();
                    Thread.Sleep(1000);
                }
                catch (KeeperException.OperationTimeoutException)
                {
                    Dispose();
                    _zk = Singleton.Instance.ZkClient();
                    Thread.Sleep(1000);
                }
                catch (TimeoutException)
                {
                    Dispose();
                    _zk = Singleton.Instance.ZkClient();
                    Thread.Sleep(1000);
                }
                //跳过NoNodeException的异常情况-相当子节点不存在的情况
                catch (KeeperException.NoNodeException)
                {
                    return null;
                }
                catch (Exception)
                {

                    throw;
                }

            }
        }
        private  void CreateSubNode(string address, string groupNode, string subNode)
        {
            Console.WriteLine(address);
            while (true)
            {
                try
                {
                    _zk.Create("/" + groupNode + "/" + subNode, address.GetBytes(), Ids.OPEN_ACL_UNSAFE,
                        CreateMode.EphemeralSequential);
                    //注册完成后--退出
                    return;
                }
                    //ConnectionLossException-Sleep 1秒
                catch (KeeperException.ConnectionLossException)
                {
                    Thread.Sleep(1000);
                }
                catch (KeeperException.SessionExpiredException)
                {
                    Dispose();
                    _zk = Singleton.Instance.ZkClient();
                    Thread.Sleep(1000);
                }
                catch (KeeperException.OperationTimeoutException)
                {
                    Dispose();
                    _zk = Singleton.Instance.ZkClient();
                    Thread.Sleep(1000);
                }
                catch (TimeoutException)
                {
                    Dispose();
                    _zk = Singleton.Instance.ZkClient();
                    Thread.Sleep(1000);
                }
                catch (Exception)
                {
                    //TODO 记录错误日志，但不抛出异常
                    //System.TimeoutException
                    //The request     / sgroup / sub    xsss - aaaExpired            world anyone     timed out while waiting for a response from the server.
                    throw;
                }
            }

        }

        private  Stat IsExistsGroupNode( string groupNode)
        {
            while (true)
            {
                try
                {
                    return _zk.Exists("/" + groupNode,true);
                }
                //ConnectionLossException-Sleep 1秒
                catch (KeeperException.ConnectionLossException)
                {
                    Thread.Sleep(1000);
                }
                catch (KeeperException.SessionExpiredException)
                {                  
                    Dispose();
                    _zk = Singleton.Instance.ZkClient();
                    Thread.Sleep(1000);
                }
                catch (KeeperException.OperationTimeoutException)
                {
                    Dispose();
                    _zk = Singleton.Instance.ZkClient();
                    Thread.Sleep(1000);
                }
                catch (TimeoutException)
                {
                    Dispose();
                    _zk = Singleton.Instance.ZkClient();
                    Thread.Sleep(1000);
                }
                catch (Exception)
                {
                    //TODO 记录错误日志，但不抛出异常
                    //System.TimeoutException
                    //The request     / sgroup / sub    xsss - aaaExpired            world anyone     timed out while waiting for a response from the server.
                    throw;
                }
            }
        }

        private  void CreateGroupNode( string groupNode)
        {
            while (true)
            {
                try
                {
                    _zk.Create("/" + groupNode, "XX集群父节点".GetBytes(), Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
                    return;
                }
                //ConnectionLossException-Sleep 1秒
                catch (KeeperException.ConnectionLossException)
                {
                    Thread.Sleep(1000);
                }
                catch (KeeperException.SessionExpiredException)
                {
                    Dispose();
                    _zk = Singleton.Instance.ZkClient();
                    Thread.Sleep(1000);
                }
                catch (KeeperException.OperationTimeoutException)
                {
                    Dispose();
                    _zk = Singleton.Instance.ZkClient();
                    Thread.Sleep(1000);
                }
                catch (TimeoutException)
                {
                    Dispose();
                    _zk = Singleton.Instance.ZkClient();
                    Thread.Sleep(1000);
                }
                //KeeperException.NodeExistsException节点存在--这是一种特殊情况
                catch (KeeperException.NodeExistsException)
                {
                    return;
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }

        public void Dispose()
        {
            Singleton.Instance.Dispose();
        }
    }
}