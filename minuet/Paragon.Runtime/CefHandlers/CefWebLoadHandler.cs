﻿//Licensed to the Apache Software Foundation(ASF) under one
//or more contributor license agreements.See the NOTICE file
//distributed with this work for additional information
//regarding copyright ownership.The ASF licenses this file
//to you under the Apache License, Version 2.0 (the
//"License"); you may not use this file except in compliance
//with the License.  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing,
//software distributed under the License is distributed on an
//"AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
//KIND, either express or implied.  See the License for the
//specific language governing permissions and limitations
//under the License.

using Xilium.CefGlue;

namespace Paragon.Runtime
{
    internal sealed class CefWebLoadHandler : CefLoadHandler
    {
        private readonly ICefWebBrowserInternal _core;

        public CefWebLoadHandler(ICefWebBrowserInternal core)
        {
            _core = core;
        }

        protected override void OnLoadStart(CefBrowser browser, CefFrame frame, CefTransitionType transition_type)
        {
            _core.OnLoadStart(new LoadStartEventArgs(frame));
        }

        protected override void OnLoadEnd(CefBrowser browser, CefFrame frame, int httpStatusCode)
        {
            _core.OnLoadEnd(new LoadEndEventArgs(frame));
        }

        protected override void OnLoadError(CefBrowser browser, CefFrame frame, CefErrorCode errorCode, string errorText, string failedUrl)
        {
            _core.OnLoadError(new LoadErrorEventArgs(frame, errorCode, errorText, failedUrl));
        }
    }
}