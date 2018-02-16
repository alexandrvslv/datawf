// 
// ViKeyNotation.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Xwt;
using Xwt.Drawing;
using System.Collections.Generic;
using System.Text;

namespace Mono.TextEditor.Vi
{
    public struct ViKey : IEquatable<ViKey>
    {
        Xwt.ModifierKeys modifiers;
        char ch;
        Key key;

        public ViKey(char ch) : this(ModifierKeys.None, ch, (Key)0)
        {
        }

        public ViKey(Key key) : this(ModifierKeys.None, '\0', key)
        {
        }

        public ViKey(ModifierKeys modifiers, Key key) : this(modifiers, '\0', key)
        {
        }

        public ViKey(ModifierKeys modifiers, char ch) : this(modifiers, ch, (Key)0)
        {
        }

        ViKey(ModifierKeys modifiers, char ch, Key key) : this()
        {
            this.modifiers = modifiers & KnownModifiers;
            this.ch = ch;
            this.key = key;
        }

        public ModifierKeys Modifiers { get { return this.modifiers; } }
        public char Char { get { return this.ch; } }
        public Key Key { get { return this.key; } }

        static ModifierKeys KnownModifiers = ModifierKeys.Shift | ModifierKeys.Alt
                                                         | ModifierKeys.Control | ModifierKeys.Command;

        public static implicit operator ViKey(char ch)
        {
            return new ViKey(ch);
        }

        public static implicit operator ViKey(Key key)
        {
            return new ViKey(key);
        }

        public bool Equals(ViKey other)
        {
            return modifiers == other.modifiers && ch == other.ch && key == other.key;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (object.Equals(this, obj))
                return true;
            if (!(obj is ViKey))
                return false;
            return Equals((ViKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return modifiers.GetHashCode() ^ ch.GetHashCode() ^ key.GetHashCode();
            }
        }

        public override string ToString()
        {
            return ViKeyNotation.IsValid(this) ? ViKeyNotation.ToString(this) : "<Invalid>";
        }
    }

    public static class ViKeyNotation
    {
        public static string ToString(ViKey key)
        {
            var sb = new StringBuilder();
            ViKeyNotation.AppendToString(key, sb);
            return sb.ToString();
        }

        public static string ToString(IList<ViKey> keys)
        {
            var sb = new StringBuilder();
            foreach (var k in keys)
                AppendToString(k, sb);
            return sb.ToString();
        }

        static void AppendToString(ViKey key, StringBuilder sb)
        {
            var c = GetString(key.Char);
            if (c != null && key.Char != '\0')
            {
                if (c.Length == 1 && key.Modifiers == ModifierKeys.None)
                {
                    sb.Append(c);
                    return;
                }
            }
            else
            {
                c = GetString(key.Key);
            }

            if (c == null)
            {
                var msg = string.Format("Invalid key char=0x{0:x} key={1}", (int)key.Char, key.Key);
                throw new InvalidOperationException(msg);
            }

            sb.Append("<");

            if ((key.Modifiers & ModifierKeys.Shift) != 0)
                sb.Append("S-");
            if ((key.Modifiers & ModifierKeys.Control) != 0)
                sb.Append("C-");
            if ((key.Modifiers & ModifierKeys.Alt) != 0)
                sb.Append("M-");
            if ((key.Modifiers & ModifierKeys.Command) != 0) //HACK: Mac command key
                sb.Append("D-");

            sb.Append(c);
            sb.Append(">");
        }

        public static bool IsValid(ViKey key)
        {
            return GetString(key.Char) != null || keyStringMaps.ContainsKey(key.Key);
        }

        static string GetString(char ch)
        {
            string s;
            if (charStringMaps.TryGetValue(ch, out s))
                return s;
            if (char.IsControl(ch))
                return null;
            return ch.ToString();
        }

        static Dictionary<char, string> charStringMaps = new Dictionary<char, string>() {
            { '\0', "Nul" },
            { ' ',  "Space" },
            { '\r', "CR" },
            { '\n', "NL" },
            { '\f', "FF" },
            { '\t', "Tab" },
            { '<',  "lt" },
            { '\\', "Bslash" },
            { '|',  "Bar" },
        };

        static string GetString(Key key)
        {
            string str;
            if (keyStringMaps.TryGetValue(key, out str))
                return str;
            return null;
        }

        static Dictionary<Key, string> keyStringMaps = new Dictionary<Key, string>() {
            { Key.BackSpace,    "BS"        },
            { Key.Tab,          "Tab"       },
            { Key.Return,       "Enter"     }, //"CR" "Return"
			{ Key.Escape,       "Esc"       },
            { Key.Space,        "Space"     },
            { Key.NumPadUp,        "Up"        },
            { Key.Up,           "Up"        },
            { Key.NumPadDown,      "Down"      },
            { Key.Down,         "Down"      },
            { Key.NumPadLeft,      "Left"      },
            { Key.Left,         "Left"      },
            { Key.NumPadRight,     "Right"     },
            { Key.Right,        "Right"     },
            { Key.F1,           "F1"        },
            { Key.F2,           "F2"        },
            { Key.F3,           "F3"        },
            { Key.F4,           "F4"        },
            { Key.F5,           "F5"        },
            { Key.F6,           "F6"        },
            { Key.F7,           "F7"        },
            { Key.F8,           "F8"        },
            { Key.F9,           "F9"        },
            { Key.F10,          "F10"       },
            //{ Key.F11,          "F11"       },
            //{ Key.F12,          "F12"       },
            { Key.Insert,       "Insert"    },
            { Key.Delete,       "Del"       },
            { Key.NumPadDelete,    "kDel"      },
            { Key.Home,         "Home"      },
            { Key.End,          "End"       },
            { Key.PageUp,      "PageUp"    },
            { Key.PageDown,    "PageDown"  },
            { Key.NumPadHome,      "kHome"     },
            { Key.NumPadEnd,       "kEnd"      },
            { Key.NumPadAdd,       "kPlus"     },
            { Key.NumPadSubtract,  "kMinus"    },
            { Key.NumPadMultiply,  "kMultiply" },
            { Key.NumPadDivide,    "kDivide"   },
            { Key.NumPadEnter,     "kEnter"    },
            { Key.NumPadDecimal,   "kPoint"    },
            { Key.NumPad0,         "k0"        },
            { Key.NumPad1,         "k1"        },
            { Key.NumPad2,         "k2"        },
            { Key.NumPad3,         "k3"        },
            { Key.NumPad4,         "k4"        },
            { Key.NumPad5,         "k5"        },
            { Key.NumPad6,         "k6"        },
            { Key.NumPad7,         "k7"        },
            { Key.NumPad8,         "k8"        },
            { Key.NumPad9,         "k9"        },
            { Key.Help,         "Help"      },
            { Key.Undo,         "Undo"      },
        };

        static char GetChar(string charName)
        {
            if (charName.Length == 1)
                return charName[0];

            char c;
            if (stringCharMaps.TryGetValue(charName, out c))
                return c;

            //FIXME this should be environment/editor-dependent
            if (charName == "EOL")
                return '\n';

            throw new FormatException("Unknown char '" + charName + "'");
        }

        static Dictionary<string, char> stringCharMaps = new Dictionary<string, char>() {
            { "Nul",     '\0' },
            { "Space",   ' '  },
            { "CR",      '\r' },
            { "NL",      '\n' },
            { "FF",      '\f' },
            { "Tab",     '\t' },
            { "lt",      '<'  },
            { "Bslash",  '\\' },
            { "Bar",     '|'  },
        };

        static Key GetKey(string code)
        {
            Key k;
            if (stringKeyMaps.TryGetValue(code, out k))
                return k;
            return (Key)0;
        }

        static Dictionary<string, Key> stringKeyMaps = new Dictionary<string, Key>() {
            { "BS",        Key.BackSpace    },
            { "Tab",       Key.Tab          },
            { "CR",        Key.Return       },
            { "Return",    Key.Return       },
            { "Enter",     Key.Return       },
            { "Esc",       Key.Escape       },
            { "Space",     Key.Space        },
            { "Up",        Key.Up           },
            { "Down",      Key.Down         },
            { "Left",      Key.Left         },
            { "Right",     Key.Right        },
            { "#1",        Key.F1           },
            { "F1",        Key.F1           },
            { "#2",        Key.F2           },
            { "F2",        Key.F2           },
            { "#3",        Key.F3           },
            { "F3",        Key.F3           },
            { "#4",        Key.F4           },
            { "F4",        Key.F4           },
            { "#5",        Key.F5           },
            { "F5",        Key.F5           },
            { "#6",        Key.F6           },
            { "F6",        Key.F6           },
            { "#7",        Key.F7           },
            { "F7",        Key.F7           },
            { "#8",        Key.F8           },
            { "F8",        Key.F8           },
            { "#9",        Key.F9           },
            { "F9",        Key.F9           },
            { "#0",        Key.F10          },
            { "F10",       Key.F10          },
            //{ "F11",       Key.F11          },
            //{ "F12",       Key.F12          },
            { "Insert",    Key.Insert       },
            { "Del",       Key.Delete       },
            { "Home",      Key.Home         },
            { "End",       Key.End          },
            { "PageUp",    Key.PageUp      },
            { "PageDown",  Key.PageDown    },
            { "kHome",     Key.NumPadHome      },
            { "kEnd",      Key.NumPadEnd       },
            { "kPlus",     Key.NumPadAdd       },
            { "kMinus",    Key.NumPadSubtract  },
            { "kMultiply", Key.NumPadMultiply  },
            { "kDivide",   Key.NumPadDivide    },
            { "kEnter",    Key.NumPadEnter     },
            { "kPoint",    Key.NumPadDecimal   },
            { "k0",        Key.NumPad0         },
            { "k1",        Key.NumPad1         },
            { "k2",        Key.NumPad2         },
            { "k3",        Key.NumPad3         },
            { "k4",        Key.NumPad4         },
            { "k5",        Key.NumPad5         },
            { "k6",        Key.NumPad6         },
            { "k7",        Key.NumPad7         },
            { "k8",        Key.NumPad8         },
            { "k9",        Key.NumPad9         },
            { "Help",      Key.Help         },
            { "Undo",      Key.Undo         },
        };

        static ViKey FlattenControlMappings(ViKey k)
        {
            ViKey ret;
            if ((k.Modifiers & ModifierKeys.Control) == k.Modifiers &&
                controlMappings.TryGetValue(k.Char, out ret))
                return ret;
            return k;
        }

        static Dictionary<uint, ViKey> controlMappings = new Dictionary<uint, ViKey>() {
            { '@', '\0'          },
            { 'h', Key.BackSpace },
            { 'i', '\t'          },
            { 'j', '\n'          },
            { 'l', '\f'          },
            { 'm', '\r'          },
            { '[', Key.Escape    },
            { 'p', Key.Up        },
        };

        public static IList<ViKey> Parse(string command)
        {
            var list = new List<ViKey>();
            for (int i = 0; i < command.Length; i++)
            {
                if (command[i] == '<')
                {
                    int j = command.IndexOf('>', i);
                    if (j < i + 2)
                        throw new FormatException("Could not find matching > at index " + i.ToString());
                    string seq = command.Substring(i + 1, j - i - 1);
                    list.Add(ParseKeySequence(seq));
                    i = j;
                }
                else
                {
                    list.Add(command[i]);
                }
            }
            return list;
        }

        //TODO: <CSI> <xCSI>
        static ViKey ParseKeySequence(string seq)
        {
            var modifiers = ModifierKeys.None;
            while (seq.Length > 2 && seq[1] == '-')
            {
                switch (seq[0])
                {
                    case 'S':
                        modifiers |= ModifierKeys.Shift;
                        break;
                    case 'C':
                        modifiers |= ModifierKeys.Control;
                        break;
                    case 'M':
                    case 'A':
                        modifiers |= ModifierKeys.Alt;
                        break;
                    case 'D':
                        modifiers |= ModifierKeys.Command;  //HACK: Mac command key
                        break;
                    default:
                        throw new FormatException("Unknown modifier " + seq[0].ToString());
                }
                seq = seq.Substring(2);
            }
            var k = GetKey(seq);
            var c = '\0';
            if (k == (Key)0)
                c = GetChar(seq);

            var key = c == '\0' ? new ViKey(modifiers, k) : new ViKey(modifiers, c);
            if (!IsValid(key))
                throw new FormatException("Invalid key sequence '" + seq + "'");
            return key;
        }
    }
}

