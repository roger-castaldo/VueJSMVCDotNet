using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    internal struct sPathPortion : IComparable
    {
        private string _path;
        public string Path
        {
            get { return _path; }
        }
        private bool _isEnd;
        private List<object> subPortions;

        public sPathPortion(string[] path, int index)
        {
            _path = path[index];
            subPortions = new List<object>();
            if (path.Length > index + 1)
            {
                _isEnd = false;
                subPortions.Add(new sPathPortion(path, index + 1));
            }
            else
                _isEnd = true;
        }

        public void MergeInPath(string[] path, int index)
        {
            if (path.Length > index + 1)
            {
                bool add = true;
                foreach (sPathPortion spp in subPortions)
                {
                    if (spp.Path == path[index + 1])
                    {
                        spp.MergeInPath(path, index + 1);
                        add = false;
                        break;
                    }
                }
                if (add)
                {
                    subPortions.Add(new sPathPortion(path, index + 1));
                    subPortions.Sort();
                }
            }
            else
                _isEnd = true;
        }

        public bool IsMatch(string[] path, int index)
        {
            if (path[index] == _path || (_path.StartsWith("{") && _path.EndsWith("}")))
            {
                if (path[index] == _path && _isEnd && index == path.Length - 1)
                    return true;
                else if (path.Length > index + 1)
                {
                    foreach (sPathPortion por in subPortions)
                    {
                        if (por.IsMatch(path, index + 1))
                            return true;
                    }
                }
                return _isEnd;
            }
            return false;
        }

        public int CompareTo(object obj)
        {
            sPathPortion por = (sPathPortion)obj;
            if (Path.StartsWith("{") && Path.EndsWith("}"))
            {
                if (por.Path.StartsWith("{") && por.Path.EndsWith("}"))
                {
                    if (_isEnd)
                        return 1;
                    else if (por._isEnd)
                        return -1;
                    else
                        return -0;
                }
                else
                    return 1;
            }
            else if (por.Path.StartsWith("{") && por.Path.EndsWith("}"))
                return -1;
            else
                return Path.CompareTo(por.Path);
        }
    }
}
