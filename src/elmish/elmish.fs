namespace Elmish

open System

/// Dispatch - feed new message into the processing loop
type Dispatch<'msg> = 'msg -> unit
/// Subscriber - return immediately, but may schedule dispatch of a message at any time
type Sub<'msg> = 'msg Dispatch -> unit
/// Cmd - container for subscriptions that may produce messages
type Cmd<'msg> = list<'msg Sub>

/// Cmd module creating and manipulating actions
/// may produce one or more message(s)
[<RequireQualifiedAccess>]
module Cmd =
    /// None - no commands, also known as `[]`
    let none : Cmd<'msg> =
        []

    /// Command to issue a specific message
    let ofMsg (msg:'msg) =
        [fun (dispatch:'msg Dispatch) -> dispatch msg]

    /// When emitting the message, map to another type
    let map (f:'a -> 'msg) (cmd:Cmd<'a>) : Cmd<'msg> =
        cmd |> List.map (fun g -> (fun post -> f >> post) >> g)

    /// Aggregate multiple commands
    let batch (cmds:list<'msg Cmd>) : Cmd<'msg> =
        List.collect id cmds

    /// Command that will evaluate async block and map the result
    /// into success or error (of exception)
    let ofAsync (task:'a->Async<_>) (arg:'a) (ofSuccess:_->'msg) (ofError:_->'msg) : Cmd<'msg> =
        let bind dispatch =
            async {
                let! r = task arg |> Async.Catch
                dispatch (match r with
                         | Choice1Of2 x -> ofSuccess x
                         | Choice2Of2 x -> ofError x)
            }
        [bind >> Async.StartImmediate]

    /// Command to evaluate a simple function and map the result
    /// into success or error (of exception)
    let ofFunc (task:'a->_) (arg:'a) (ofSuccess:_->'msg) (ofError:_->'msg) : Cmd<'msg> =
        let bind (dispatch:'msg -> unit) =
            try
                task arg
                |> (ofSuccess >> dispatch)
            with x ->
                x |> (ofError >> dispatch)
        [bind]

    /// Command to call the subscriber
    let ofSub (sub:Sub<'msg>) =
        [sub]

    open Fable.PowerPack

    /// Command to call `promise` block and map the results
    let ofPromise (task:'a->Fable.Import.JS.Promise<_>) (arg:'a) (ofSuccess:_->'msg) (ofError:Exception->'msg) : Cmd<'msg> =
        let bind (dispatch:'msg -> unit) =
            task arg
            |> Promise.map (ofSuccess >> dispatch)
            |> Promise.catch (ofError >> dispatch)
            |> ignore
        [bind]


type Program<'arg,'model,'msg, 'view> = {
    init : 'arg -> 'model * Cmd<'msg>
    update : 'msg -> 'model -> 'model * Cmd<'msg>
    subscribe : 'model -> Cmd<'msg>
    view : 'model -> Dispatch<'msg> -> 'view
}

/// Program module - functions to manipulate program instances
module Program =
    /// Typical program, produces new commands as part of init() and update() as well as the new model.
    let mkProgram
        (init:'arg -> 'model * Cmd<'msg>)
        (update:'msg -> 'model -> 'model * Cmd<'msg>)
        (view : 'model -> Dispatch<'msg> -> 'view) =
        { init = init
          update = update
          view = view
          subscribe = fun _ -> Cmd.none }

    /// Simple program that produces only new model in init() and update().
    /// Good for tutorials
    let mkSimple
        (init:'arg -> 'model)
        (update:'msg -> 'model -> 'model)
        (view : 'model -> Dispatch<'msg> -> 'view) =
        { init = init >> fun state -> state,Cmd.none
          update = fun msg -> update msg >> fun state -> state,Cmd.none
          view = view
          subscribe = fun _ -> Cmd.none }

    /// Subscribe to external source of events.
    /// The subscriptions are called once - with the initial model, but can call dispatch whenever they need.
    let withSubscription (subscribe : 'model -> Cmd<'msg>) (program:Program<'arg,'model,'msg,'view>) =
        { program with subscribe = subscribe }

    /// Trace all the updates to the console
    let withConsoleTrace (program:Program<'arg,'model,'msg,'view>) =
        let trace text msg model =
            Fable.Import.Browser.console.log (text, model, msg)
            program.update msg model
        { program with update = trace "Updating:"}

    /// Trace all the messages as they update the model
    let withTrace (program:Program<'arg,'model,'msg,'view>) trace =
        { program
            with update = fun msg model -> trace msg model; program.update msg model}

    /// Start the program loop.
    /// arg: argument to pass to the init() function.
    /// setState: function that will be called with the new model state and the dispatch function to feed new messages into the loop.
    /// program: program created with 'mkSimple' or 'mkProgram'.
    let runWith (arg:'arg) (setState:'model->'msg Dispatch->unit) (program:Program<'arg,'model,'msg,'view>) =
        let (model,cmd) = program.init arg
        let inbox = MailboxProcessor.Start(fun (mb:MailboxProcessor<'msg>) ->
            let rec loop (state:'model) =
                async {
                    let! msg = mb.Receive()
                    try
                        let (model',cmd') = program.update msg state
                        setState model' mb.Post
                        cmd' |> List.iter (fun sub -> sub mb.Post)
                        return! loop model'
                    with ex ->
                        Fable.Import.Browser.console.error ("unable to process a message:", ex)
                        return! loop state
                }
            loop model
        )
        setState model inbox.Post
        program.subscribe model
        @ cmd |> List.iter (fun sub -> sub inbox.Post)

    /// Start the dispatch loop with `unit` for the init() function.
    let run setState (program:Program<unit,'model,'msg,'view>) = runWith () setState program

    /// Start the dispatch loop with `unit` for the init() function and use the program.view to set state
    let runWithView (program:Program<unit,'model,'msg,_>) = runWith () program.view program

