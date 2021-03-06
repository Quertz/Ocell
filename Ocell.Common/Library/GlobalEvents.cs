﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Threading;

namespace Ocell.Library
{
    public static class GlobalEvents
    {
        public static event EventHandler FiltersChanged;

        public static void FireFiltersChanged(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem((threadcontext) => {
                if (FiltersChanged != null)
                    FiltersChanged(sender, e);
            });
        }
    }
}
