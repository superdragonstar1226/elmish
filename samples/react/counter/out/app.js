var _createClass = function () { function defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } } return function (Constructor, protoProps, staticProps) { if (protoProps) defineProperties(Constructor.prototype, protoProps); if (staticProps) defineProperties(Constructor, staticProps); return Constructor; }; }();

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

import _Symbol from "fable-core/Symbol";
import { declare, compareUnions, equalsUnions } from "fable-core/Util";
import { createElement } from "react";
import { fold } from "fable-core/Seq";
import { ProgramModule } from "../../node_modules/fable-elmish/elmish";
import { withReact } from "../../node_modules/fable-elmish-react/react";
export var Msg = function () {
  function Msg(caseName, fields) {
    _classCallCheck(this, Msg);

    this.Case = caseName;
    this.Fields = fields;
  }

  _createClass(Msg, [{
    key: _Symbol.reflection,
    value: function value() {
      return {
        type: "App.Msg",
        interfaces: ["FSharpUnion", "System.IEquatable", "System.IComparable"],
        cases: {
          Decrement: [],
          Increment: []
        }
      };
    }
  }, {
    key: "Equals",
    value: function Equals(other) {
      return equalsUnions(this, other);
    }
  }, {
    key: "CompareTo",
    value: function CompareTo(other) {
      return compareUnions(this, other);
    }
  }]);

  return Msg;
}();
declare(Msg);
export function init() {
  return 0;
}
export function update(msg, count) {
  return msg.Case === "Decrement" ? count - 1 : count + 1;
}
export function view(count, dispatch) {
  var onClick = function onClick(msg) {
    return ["onClick", function (_arg1) {
      dispatch(msg);
    }];
  };

  return createElement("div", {}, createElement("button", fold(function (o, kv) {
    o[kv[0]] = kv[1];
    return o;
  }, {}, [onClick(new Msg("Decrement", []))]), "-"), createElement("div", {}, String(count)), createElement("button", fold(function (o, kv) {
    o[kv[0]] = kv[1];
    return o;
  }, {}, [onClick(new Msg("Increment", []))]), "+"));
}
ProgramModule.run(withReact("elmish-app", ProgramModule.withConsoleTrace(ProgramModule.mkSimple(function () {
  return init();
}, function (msg) {
  return function (count) {
    return update(msg, count);
  };
}, function (count) {
  return function (dispatch) {
    return view(count, dispatch);
  };
}))));
//# sourceMappingURL=app.js.map