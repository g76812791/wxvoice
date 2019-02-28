using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


public static class HtmlExtensions
{
    public delegate void SelfApplicable<T>(SelfApplicable<T> self, T arg);

    public static void Render<T>(this HtmlHelper helper, T model, SelfApplicable<T> f)
    {
        f(f, model);
    }
}
