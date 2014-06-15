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
      return parseError("noimpl");
    } else if (str[0] == kQuote) {
      return parseError("noimpl");
    }
    return readAtom(str);
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
