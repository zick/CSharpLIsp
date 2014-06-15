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

  public static LObj pairlis(LObj lst1, LObj lst2) {
    LObj ret = kNil;
    while (lst1.tag() == Type.Cons && lst2.tag() == Type.Cons) {
      ret = makeCons(makeCons(lst1.cons().car, lst2.cons().car), ret);
      lst1 = lst1.cons().cdr;
      lst2 = lst2.cons().cdr;
    }
    return nreverse(ret);
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

class Evaluator {
  private static LObj findVar(LObj sym, LObj env) {
    while (env.tag() == Type.Cons) {
      LObj alist = env.cons().car;
      while (alist.tag() == Type.Cons) {
        if (alist.cons().car.cons().car == sym) {
          return alist.cons().car;
        }
        alist = alist.cons().cdr;
      }
      env = env.cons().cdr;
    }
    return Util.kNil;
  }

  public static void addToEnv(LObj sym, LObj val, LObj env) {
    env.cons().car = Util.makeCons(Util.makeCons(sym, val), env.cons().car);
  }

  public static LObj eval(LObj obj, LObj env) {
    if (obj.tag() == Type.Nil || obj.tag() == Type.Num ||
        obj.tag() == Type.Error) {
      return obj;
    } else if (obj.tag() == Type.Sym) {
      LObj bind = findVar(obj, env);
      if (bind == Util.kNil) {
        return Util.makeError(obj.str() + " has no value");
      }
      return bind.cons().cdr;
    }

    LObj op = Util.safeCar(obj);
    LObj args = Util.safeCdr(obj);
    if (op == Util.makeSym("quote")) {
      return Util.safeCar(args);
    } else if (op == Util.makeSym("if")) {
      if (eval(Util.safeCar(args), env) == Util.kNil) {
        return eval(Util.safeCar(Util.safeCdr(Util.safeCdr(args))), env);
      }
      return eval(Util.safeCar(Util.safeCdr(args)), env);
    } else if (op == Util.makeSym("lambda")) {
      return Util.makeExpr(args, env);
    } else if (op == Util.makeSym("defun")) {
      LObj expr = Util.makeExpr(Util.safeCdr(args), env);
      LObj sym = Util.safeCar(args);
      addToEnv(sym, expr, gEnv);
      return sym;
    }
    return apply(eval(op, env), evlis(args, env), env);
  }

  private static LObj evlis(LObj lst, LObj env) {
    LObj ret = Util.kNil;
    while (lst.tag() == Type.Cons) {
      LObj elm = eval(lst.cons().car, env);
      if (elm.tag() == Type.Error) {
        return elm;
      }
      ret = Util.makeCons(elm, ret);
      lst = lst.cons().cdr;
    }
    return Util.nreverse(ret);
  }

  private static LObj progn(LObj body, LObj env) {
    LObj ret = Util.kNil;
    while (body.tag() == Type.Cons) {
      ret = eval(body.cons().car, env);
      body = body.cons().cdr;
    }
    return ret;
  }

  private static LObj apply(LObj fn, LObj args, LObj env) {
    if (fn.tag() == Type.Error) {
      return fn;
    } else if (args.tag() == Type.Error) {
      return args;
    } else if (fn.tag() == Type.Subr) {
      return fn.subr()(args);
    } else if (fn.tag() == Type.Expr) {
      return progn(fn.expr().body,
                   Util.makeCons(Util.pairlis(fn.expr().args, args),
                   fn.expr().env));
    }
    return Util.makeError(fn.ToString() + " is not function");
  }

  private static LObj makeGlobalEnv() {
    LObj env = Util.makeCons(Util.kNil, Util.kNil);
    Subr subrCar = delegate(LObj args) {
      return Util.safeCar(Util.safeCar(args));
    };
    Subr subrCdr = delegate(LObj args) {
      return Util.safeCdr(Util.safeCar(args));
    };
    Subr subrCons = delegate(LObj args) {
      return Util.makeCons(Util.safeCar(args),
                           Util.safeCar(Util.safeCdr(args)));
    };
    addToEnv(Util.makeSym("car"), Util.makeSubr(subrCar), env);
    addToEnv(Util.makeSym("cdr"), Util.makeSubr(subrCdr), env);
    addToEnv(Util.makeSym("cons"), Util.makeSubr(subrCons), env);
    addToEnv(Util.makeSym("t"), Util.makeSym("t"), env);
    return env;
  }

  public static LObj globalEnv() { return gEnv; }

  private static LObj gEnv = makeGlobalEnv();
}

class Lisp {
  static void Main() {
    LObj gEnv = Evaluator.globalEnv();
    string line;
    Console.Write("> ");
    while ((line = Console.In.ReadLine()) != null) {
      Console.Write(Evaluator.eval(Reader.read(line).obj, gEnv));
      Console.Write("\n> ");
    }
  }
}
