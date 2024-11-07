using OpenXmlPowerTools;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Sahadeva.Dossier.DocumentGenerator.OpenXml
{
    // In OpenXML, a new <w:r> element (which represents a "run" of text) is created in a Word document whenever there is a change in formatting
    // or a break in the text flow. Runs are the smallest unit of text that can have individual formatting applied.
    // Sometimes Word will split a placeholder text into multiple runs and so it is not always possible to perform a simple
    // search to find the placeholder. The code in this file is copied from https://github.com/OpenXmlDev/Open-Xml-PowerTools/blob/921cbccf6ecb29456eedc8d4d7834c7bf62c54c9/OpenXmlPowerTools/OpenXmlRegex.cs
    // It has been modified to merge the placeholders that may be split across multiple runs into a single text node, which we can
    // then process further
    internal class RegexHelper
    {
        private class ReplaceInternalInfo
        {
            public int Count;
        }


        private static readonly HashSet<XName> RevTrackMarkupWithId = new HashSet<XName>
        {
            W.cellDel,
            W.cellIns,
            W.cellMerge,
            W.customXmlDelRangeEnd,
            W.customXmlDelRangeStart,
            W.customXmlInsRangeEnd,
            W.customXmlInsRangeStart,
            W.customXmlMoveFromRangeEnd,
            W.customXmlMoveFromRangeStart,
            W.customXmlMoveToRangeEnd,
            W.customXmlMoveToRangeStart,
            W.del,
            W.ins,
            W.moveFrom,
            W.moveFromRangeEnd,
            W.moveFromRangeStart,
            W.moveTo,
            W.moveToRangeEnd,
            W.moveToRangeStart,
            W.pPrChange,
            W.rPrChange,
            W.sectPrChange,
            W.tblGridChange,
            W.tblPrChange,
            W.tblPrExChange,
            W.tcPrChange
        };

        internal static int Replace(IEnumerable<XElement> content, Regex regex, Func<Match, string> replacement, Func<XElement, Match, bool> callback = null)
        {
            if (content == null)
            {
                throw new ArgumentNullException("content");
            }
            if (regex == null)
            {
                throw new ArgumentNullException("regex");
            }
            IEnumerable<XElement> enumerable = (content as IList<XElement>) ?? content.ToList();
            XElement xElement = enumerable.FirstOrDefault();
            if (xElement == null)
            {
                return 0;
            }
            if (xElement.Name.Namespace == W.w)
            {
                if (!enumerable.Any())
                {
                    return 0;
                }
                ReplaceInternalInfo replaceInternalInfo = new ReplaceInternalInfo
                {
                    Count = 0
                };
                foreach (XElement item in enumerable)
                {
                    XElement xElement2 = (XElement)WmlSearchAndReplaceTransform(item, regex, replacement, callback, replaceInternalInfo);
                    item.ReplaceNodes(xElement2.Nodes());
                }
                XElement xElement3 = enumerable.First().AncestorsAndSelf().Last();
                int num = new int[1].Concat(from a in (from d in xElement3.Descendants()
                                                       where RevTrackMarkupWithId.Contains(d.Name)
                                                       select d).Attributes(W.id)
                                            select (int)a).Max() + 1;
                foreach (XElement item2 in from d in xElement3.DescendantsAndSelf()
                                           where RevTrackMarkupWithId.Contains(d.Name) && d.Attribute(W.id) == null
                                           select d)
                {
                    item2.Add(new XAttribute(W.id, num++));
                }
                foreach (IGrouping<int, XElement> item3 in (from d in xElement3.DescendantsAndSelf()
                                                            where RevTrackMarkupWithId.Contains(d.Name)
                                                            group d by (int)d.Attribute(W.id) into g
                                                            where g.Count() > 1
                                                            select g).ToList())
                {
                    foreach (XElement item4 in item3.Skip(1))
                    {
                        XAttribute xAttribute = item4.Attribute(W.id);
                        if (xAttribute != null)
                        {
                            xAttribute.Value = num.ToString();
                        }
                        num++;
                    }
                }
                return replaceInternalInfo.Count;
            }
            if (xElement.Name.Namespace == P.p || xElement.Name.Namespace == A.a)
            {
                ReplaceInternalInfo replaceInternalInfo2 = new ReplaceInternalInfo
                {
                    Count = 0
                };
                foreach (XElement item5 in enumerable)
                {
                    XElement xElement4 = (XElement)PmlSearchAndReplaceTransform(item5, regex, replacement, callback, replaceInternalInfo2);
                    item5.ReplaceNodes(xElement4.Nodes());
                }
                return replaceInternalInfo2.Count;
            }
            return 0;
        }

        private static object WmlSearchAndReplaceTransform(XNode node, Regex regex, Func<Match, string> replacement, Func<XElement, Match, bool> callback, ReplaceInternalInfo replInfo)
        {
            XElement element = node as XElement;
            if (element == null)
            {
                return node;
            }
            if (element.Name == W.p)
            {
                XElement xElement = element;
                string input = (from d in xElement.DescendantsTrimmed(W.txbxContent)
                                where d.Name == W.r && (d.Parent == null || d.Parent.Name != W.del)
                                select d).Select(UnicodeMapper.RunToString).StringConcatenate();
                if (regex.IsMatch(input))
                {
                    XElement xElement2 = new XElement(W.p, xElement.Attributes(), from n in xElement.Nodes()
                                                                                  select WmlSearchAndReplaceTransform(n, regex, replacement, callback, replInfo));
                    var source = (from d in xElement2.DescendantsTrimmed(W.txbxContent)
                                  where d.Name == W.r && (d.Parent == null || d.Parent.Name != W.del)
                                  select d into r
                                  select new
                                  {
                                      Ch = UnicodeMapper.RunToString(r),
                                      r = r
                                  }).ToList();
                    string input2 = source.Select(t => t.Ch).StringConcatenate();
                    XElement[] source2 = source.Select(t => t.r).ToArray();
                    MatchCollection matchCollection = regex.Matches(input2);
                    replInfo.Count += matchCollection.Count;
                    if (replacement == null)
                    {
                        if (callback == null)
                        {
                            return xElement;
                        }
                        {
                            foreach (Match item in matchCollection.Cast<Match>())
                            {
                                callback(xElement, item);
                            }
                            return xElement;
                        }
                    }
                    foreach (Match item2 in matchCollection.Cast<Match>())
                    {
                        if (item2.Length == 0 || (callback != null && !callback(xElement, item2)))
                        {
                            continue;
                        }
                        var replacementText = replacement(item2);
                        List<XElement> list = source2.Skip(item2.Index).Take(item2.Length).ToList();
                        XElement xElement3 = list.First();
                        XElement runProperties = xElement3.Elements(W.rPr).FirstOrDefault();
                        foreach (XElement item4 in list.Skip(1).ToList())
                        {
                            if (item4.Parent != null && item4.Parent.Name == W.ins)
                            {
                                item4.Parent.Remove();
                            }
                            else
                            {
                                item4.Remove();
                            }
                        }
                        List<XElement> content5 = UnicodeMapper.StringToCoalescedRunList(item2.Result(replacementText), runProperties);
                        if (xElement3.Parent != null && xElement3.Parent.Name == W.ins)
                        {
                            xElement3.Parent.ReplaceWith(content5);
                        }
                        else
                        {
                            xElement3.ReplaceWith(content5);
                        }
                    }
                    return WordprocessingMLUtil.CoalesceAdjacentRunsWithIdenticalFormatting(xElement2);
                }
                XElement xElement4 = new XElement(W.p, xElement.Attributes(), xElement.Nodes().Select(delegate (XNode n)
                {
                    if (!(n is XElement xElement5))
                    {
                        return n;
                    }
                    if (xElement5.Name == W.pPr)
                    {
                        return xElement5;
                    }
                    if ((xElement5.Name == W.r && xElement5.Elements(W.t).Any()) || xElement5.Elements(W.tab).Any())
                    {
                        return xElement5;
                    }
                    return (xElement5.Name == W.ins && xElement5.Elements(W.r).Elements(W.t).Any()) ? xElement5 : WmlSearchAndReplaceTransform(xElement5, regex, replacement, callback, replInfo);
                }));
                return WordprocessingMLUtil.CoalesceAdjacentRunsWithIdenticalFormatting(xElement4);
            }
            if (element.Name == W.ins && element.Elements(W.r).Any())
            {
                return (from c in (from n in element.Elements()
                                   select WmlSearchAndReplaceTransform(n, regex, replacement, callback, replInfo)).ToList()
                        select (!(c is IEnumerable<XElement> source3)) ? c : source3.Select((XElement ixc) => new XElement(W.ins, element.Attributes(), ixc))).ToList();
            }
            if (element.Name == W.r)
            {
                return (from e in element.Elements()
                        where e.Name != W.rPr
                        select (!(e.Name == W.t)) ? new XElement[1]
                        {
                    new XElement(W.r, element.Elements(W.rPr), e)
                        } : ((string?)e).Select((char c) => new XElement(W.r, element.Elements(W.rPr), new XElement(W.t, XmlUtil.GetXmlSpaceAttribute(c), c)))).SelectMany((IEnumerable<XElement> t) => t);
            }
            return new XElement(element.Name, element.Attributes(), from n in element.Nodes()
                                                                    select WmlSearchAndReplaceTransform(n, regex, replacement, callback, replInfo));
        }

        private static object PmlSearchAndReplaceTransform(XNode node, Regex regex, Func<Match, string> replacement, Func<XElement, Match, bool> callback, ReplaceInternalInfo counter)
        {
            XElement element = node as XElement;
            if (element == null)
            {
                return node;
            }

            if (element.Name == A.p)
            {
                XElement xElement = element;
                string input = (from t in element.Descendants(A.t)
                                select (string?)t).StringConcatenate();
                if (!regex.IsMatch(input))
                {
                    return new XElement(element.Name, element.Attributes(), element.Nodes());
                }

                XElement xElement2 = new XElement(A.p, xElement.Attributes(), from n in xElement.Nodes()
                                                                              select PmlSearchAndReplaceTransform(n, regex, replacement, callback, counter));
                var source = (from r in xElement2.Descendants(A.r).ToList()
                              select (r.Element(A.t) == null) ? new
                              {
                                  Ch = "\u0001",
                                  r = r
                              } : new
                              {
                                  Ch = r.Element(A.t).Value,
                                  r = r
                              }).ToList();
                string input2 = source.Select(t => t.Ch).StringConcatenate();
                XElement[] source2 = source.Select(t => t.r).ToArray();
                MatchCollection matchCollection = regex.Matches(input2);
                counter.Count += matchCollection.Count;
                if (replacement == null)
                {
                    foreach (Match item in matchCollection.Cast<Match>())
                    {
                        callback(xElement, item);
                    }
                }
                else
                {
                    foreach (Match item2 in matchCollection.Cast<Match>())
                    {
                        if (callback == null || callback(xElement, item2))
                        {
                            List<XElement> source3 = source2.Skip(item2.Index).Take(item2.Length).ToList();
                            XElement xElement3 = source3.First();
                            source3.Skip(1).Remove();
                            XElement content = new XElement(A.r, xElement3.Element(A.rPr), new XElement(A.t, replacement));
                            xElement3.ReplaceWith(content);
                        }
                    }

                    IEnumerable<IGrouping<string, XElement>> source4 = xElement2.Elements().GroupAdjacent(delegate (XElement ce)
                    {
                        if (ce.Name != A.r)
                        {
                            return "DontConsolidate";
                        }

                        if (ce.Elements().Count((XElement e) => e.Name != A.rPr) != 1 || ce.Element(A.t) == null)
                        {
                            return "DontConsolidate";
                        }

                        XElement xElement4 = ce.Element(A.rPr);
                        return (xElement4 != null) ? xElement4.ToString(SaveOptions.None) : "";
                    });
                    xElement = new XElement(A.p, source4.Select((Func<IGrouping<string, XElement>, object>)delegate (IGrouping<string, XElement> g)
                    {
                        if (g.Key == "DontConsolidate")
                        {
                            return g;
                        }

                        string text = g.Select((XElement r) => r.Element(A.t).Value).StringConcatenate();
                        XAttribute xmlSpaceAttribute = XmlUtil.GetXmlSpaceAttribute(text);
                        return new XElement(A.r, g.First().Elements(A.rPr), new XElement(A.t, xmlSpaceAttribute, text));
                    }));
                }

                return xElement;
            }

            if (element.Name == A.r && element.Elements(A.t).Any())
            {
                return from e in element.Elements()
                       where e.Name != A.rPr
                       select (e.Name == A.t) ? ((object)((string?)e).Select((char c) => new XElement(A.r, element.Elements(A.rPr), new XElement(A.t, XmlUtil.GetXmlSpaceAttribute(c), c)))) : ((object)new XElement(A.r, element.Elements(A.rPr), e));
            }

            return new XElement(element.Name, element.Attributes(), from n in element.Nodes()
                                                                    select PmlSearchAndReplaceTransform(n, regex, replacement, callback, counter));
        }

        private static object TransformToDelText(XNode node)
        {
            if (!(node is XElement xElement))
            {
                return node;
            }

            if (xElement.Name == W.t)
            {
                return new XElement(W.delText, XmlUtil.GetXmlSpaceAttribute(xElement.Value), xElement.Value);
            }

            return new XElement(xElement.Name, xElement.Attributes(), xElement.Nodes().Select(TransformToDelText));
        }
    }
}
