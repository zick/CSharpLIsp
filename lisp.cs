using System;
using System.Collections;
using System.IO;

enum Type { Nil, Num, Sym, Error, Cons, Subr, Expr };

class LObj {
  public Type tag() { return tag_; }

  public LObj(Type type, object obj) {
    tag_ = type;
    data_ = obj;
  }

  public Int32 num() {
    return (Int32)data_;
  }
  public String str() {
    return (String)data_;
  }
  public Cons cons() {
    return (Cons)data_;
  }
  public Subr subr() {
    return (Subr)data_;
  }
  public Expr expr() {
    return (Expr)data_;
  }

  public override String ToString() {
    if (tag_ == Type.Nil) {
      return "nil";
    } else if (tag_ == Type.Num) {
      return num().ToString();
    } else if (tag_ == Type.Sym) {
      return str();
    } else if (tag_ == Type.Error) {
      return "<error: " + str() + ">";
    } else if (tag_ == Type.Cons) {
      return listToString(this);
    } else if (tag_ == Type.Subr) {
      return "<subr>";
    } else if (tag_ == Type.Expr) {
      return "<expr>";
    }
    return "<unknown>";
  }

  private String listToString(LObj obj) {
    String ret = "";
    bool first = true;
    while (obj.tag() == Type.Cons) {
      if (first) {
        first = false;
      } else {
        ret += " ";
      }
      ret += obj.cons().car.ToString();
      obj = obj.cons().cdr;
    }
    if (obj.tag() == Type.Nil) {
      return "(" + ret + ")";
    }
    return "(" + ret + " . " + obj.ToString() + ")";
  }

  private Type tag_;
  private object data_;
}

class Cons {
  public Cons(LObj a, LObj d) {
    car = a;
    cdr = d;
  }
  public LObj car;
  public LObj cdr;
}

delegate LObj Subr(LObj ags);

class Expr {
  public Expr(LObj a, LObj b, LObj e) {
    args = a;
    body = b;
    env = e;
  }
  public LObj args;
  public LObj body;
  public LObj env;
}

class Util {
  public static LObj makeNum(Int32 num) {
    return new LObj(Type.Num, num);
  }
  public static LObj makeError(String str) {
    return new LObj(Type.Error, str);
  }
  public static LObj makeCons(LObj a, LObj d) {
    return new LObj(Type.Cons, new Cons(a, d));
  }
  public static LObj makeSubr(Subr subr) {
    return new LObj(Type.Subr, subr);
  }
  public static LObj makeExpr(LObj args, LObj env) {
    return new LObj(Type.Expr, new Expr(safeCar(args), safeCdr(args), env));
  }
  public static LObj makeSym(String str) {
    if (str == "nil") {
      return kNil;
    } else if (!symbolMap.Contains(str)) {
      symbolMap[str] = new LObj(Type.Sym, str);
    }
    return (LObj)symbolMap[str];
  }

  public static LObj safeCar(LObj obj) {
    if (obj.tag() == Type.Cons) {
      return obj.cons().car;
    }
    return kNil;
  }
  public static LObj safeCdr(LObj obj) {
    if (obj.tag() == Type.Cons) {
      return obj.cons().cdr;
    }
    return kNil;
  }

  public static LObj nreverse(LObj lst) {
    LObj ret = kNil;
    while (lst.tag() == Type.Cons) {
      LObj tmp = lst.cons().cdr;
      lst.cons().cdr = ret;
      ret = lst;
      lst = tmp;
    }
    return ret;
  }

  public static LObj kNil = new LObj(Type.Nil, "nil");
  private static Hashtable symbolMap = new Hashtable();
}

class ParseState {
  public ParseState(LObj o, String s) {
    obj = o;
    next = s;
  }
  public LObj obj;
  public String next;
}

class Reader {
  private static bool isSpace(Char c) {
    return c == '\t' || c == '\r' || c == '\n' || c == ' ';
  }
  private static bool isDelimiter(Char c) {
    return c == kLPar || c == kRPar || c == kQuote || isSpace(c);
  }

  private static String skipSpaces(String str) {
    int i;
    for (i = 0; i < str.Length; i++) {
      if (!isSpace(str[i])) {
        break;
      }
    }
    return str.Substring(i);
  }

  private static LObj makeNumOrSym(String str) {
    try {
      return Util.makeNum(Int32.Parse(str));
    } catch (FormatException) {
      return Util.makeSym(str);
    }
  }

  private static ParseState parseError(string s) {
    return new ParseState(Util.makeError(s), "");
  }

  private static ParseState readAtom(String str) {
    String next = "";
    for (int i = 0; i < str.Length; i++) {
      if (isDelimiter(str[i])) {
        next = str.Substring(i);
        str = str.Substring(0, i);
        break;;
      }
    }
    return new ParseState(makeNumOrSym(str), next);
  }

  public static ParseState read(String str) {
    str = skipSpaces(str);
    if (str.Length == 0) {
      return parseError("empty input");
    } else if (str[0] == kRPar) {
      return parseError("invalid syntax: " + str);
    } else if (str[0] == kLPar) {
      return readList(str.Substring(1));
    } else if (str[0] == kQuote) {
      ParseState tmp = read(str.Substring(1));
      return new ParseState(Util.makeCons(Util.makeSym("quote"),
                                          Util.makeCons(tmp.obj, Util.kNil)),
                            tmp.next);
    }
    return readAtom(str);
  }

  private static ParseState readList(String str) {
    LObj ret = Util.kNil;
    while (true) {
      str = skipSpaces(str);
      if (str.Length == 0) {
        return parseError("unfinished parenthesis");
      } else if (str[0] == kRPar) {
        break;
      }
      ParseState tmp = read(str);
      if (tmp.obj.tag() == Type.Error) {
        return tmp;
      }
      ret = Util.makeCons(tmp.obj, ret);
      str = tmp.next;
    }
    return new ParseState(Util.nreverse(ret), str.Substring(1));
  }

  private static char kLPar = '(';
  private static char kRPar = ')';
  private static char kQuote = '\'';
}

class Lisp {
  static void Main() {
    string line;
    Console.Write("> ");
    while ((line = Console.In.ReadLine()) != null) {
      Console.Write(Reader.read(line).obj);
      Console.Write("\n> ");
    }
  }
}
