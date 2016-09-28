(**
 - title: Counter
 - tagline: The famous Increment/Decrement ported from Elm
*)


#r "node_modules/fable-core/Fable.Core.dll"
#load "node_modules/fable-import-react/Fable.Import.React.fs"
#load "node_modules/fable-import-react/Fable.Helpers.React.fs"
#load "node_modules/fable-import-fetch/Fable.Import.Fetch.fs"
#load "node_modules/fable-import-fetch/Fable.Helpers.Fetch.fs"
#load "node_modules/fable-elmish/elmish.fs"
#load "node_modules/fable-elmish/elmish-browser-nav.fs"
#load "node_modules/fable-elmish/elmish-result.fs"
#load "node_modules/fable-elmish/elmish-parser.fs"

open Fable.Core
open Fable.Import
open Elmish
open Fable.Import.Browser
open Elmish.Browser.Navigation
open Elmish.UrlParser

// Types
type Page = Home | Blog of int | Search of string

type Model =
  { page : Page
    query : string
    cache : Map<string,string list> }

let toHash = 
    function
    | Home -> "#home"
    | Blog id -> "#blog/" + (string id)
    | Search query -> "#search/" + query

let pageParser : Parser<Page->_,_> =
  oneOf
    [ format Home (s "home")
      format Blog (s "blog" </> i32)
      format Search (s "search" </> str) ]

let hashParser (location:Location) =
  UrlParser.parse id pageParser (location.hash.Substring 1)

type Msg = 
  | Query of string
  | Enter
  | FetchFailure of string*exn
  | FetchSuccess of string*(string list)


type Place = { ``place name``: string; state: string }
type ZipResponse = { places : Place list }

let get query =
    async {
        let! r = Fable.Helpers.Fetch.fetchAs<ZipResponse> ("http://api.zippopotam.us/us/" + query, [])
        return r.places |> List.map (fun p -> p.``place name`` + ", " + p.state)
    }

(* The URL is turned into a result. If the URL is valid, we just update our
model to the new count. If it is not a valid URL, we modify the URL to make
sense.
*)
let urlUpdate (result:Result<Page,string>) model =
  match result with
  | Error e ->
      Browser.console.error("Error parsing url:", e)  
      ( model, Navigation.modifyUrl (toHash model.page) )

  | Ok (Search query as page) ->
      { model with page = page; query = query },
         if Map.containsKey query model.cache then [] 
         else Cmd.ofAsync get query (fun r -> FetchSuccess (query,r)) (fun ex -> FetchFailure (query,ex)) 

  | Ok page ->
      { model with page = page; query = "" }, []

let init result =
  urlUpdate result { page = Home; query = ""; cache = Map.empty }



(* A relatively normal update function. The only notable thing here is that we
are commanding a new URL to be added to the browser history. This changes the
address bar and lets us use the browser&rsquo;s back button to go back to
previous pages.
*)
let update msg model =
  match msg with
  | Query query ->
      { model with query = query }, []

  | Enter ->
      let newPage = Search model.query
      { model with page = newPage }, []// [ Navigation.newUrl (toHash newPage) ]

  | FetchFailure (query,_) ->
      { model with cache = Map.add query [] model.cache }, []

  | FetchSuccess (query,locations) -> { model with cache = Map.add query locations model.cache }, []




// VIEW

open Fable.Helpers.React
open Fable.Helpers.React.Props


let viewLink page description =
  a [ Style [ Padding "0 20px" ]
      Href (toHash page) ] 
    [ unbox description]

let internal centerStyle direction =
    Style [ Display "flex"
            FlexDirection direction
            AlignItems "center"
            unbox("justifyContent", "center")
            Padding "20px 0"
    ]

let words size message =
  span [ Style [ size |> sprintf "%dpx" |> box |> FontSizeAdjust ] ] [ unbox message ]

let internal onEnter msg dispatch =
    function 
    | (ev:React.KeyboardEvent) when ev.keyCode = 13. ->
        ev.preventDefault() 
        dispatch msg
    | _ -> ()
    |> OnKeyDown

let viewPage model dispatch =
  match model.page with
  | Home ->
      [ words 60 "Welcome!"
        unbox "Play with the links and search bar above. (Press ENTER to trigger the zip code search.)"
      ]

  | Blog id ->
      [ words 20 "This is blog post number"
        words 100 (string id)
      ]

  | Search query ->
      match Map.tryFind query model.cache with
      | Some [] ->
          [ unbox ("No results found for " + query + ". Need a valid zip code like 90210.") ]

      | Some (location :: _) ->
          [ words 20 ("Zip code " + query + " is in " + location + "!") ]
      | _ ->
          [ unbox "..." ]

open Fable.Core.JsInterop

let view model dispatch =
  div []
    [ div [ centerStyle "row" ]
        [ viewLink Home "Home"
          viewLink (Blog 42) "Cat Facts"
          viewLink (Blog 13) "Alligator Jokes"
          viewLink (Blog 26) "Workout Plan"
          input
            [ Placeholder "Enter a zip code (e.g. 90210)"
              Value (U2.Case1 model.query)
              onEnter Enter dispatch
              OnInput (fun ev -> ev.target?value |> unbox |> dispatch)
              Style [ CSSProp.Width "200px"; Margin "0 20px" ]
            ]
            []
        ]
      hr [] []
      div [ centerStyle "column" ] (viewPage model dispatch)
    ]



// App
let program = 
    Program.mkProgram init update
    |> Program.withConsoleTrace

type App() as this =
    inherit React.Component<obj, Model>()
    
    let safeState state =
        match unbox this.props with 
        | false -> this.state <- state
        | _ -> this.setState state

    let dispatch = 
        program 
        |> Program.runWithNavigation hashParser urlUpdate safeState

    member this.componentDidMount() =
        this.props <- true

    member this.render() =
        view this.state dispatch

ReactDom.render(
        com<App,_,_> () [],
        document.getElementsByClassName("elmish-app").[0]
    ) |> ignore